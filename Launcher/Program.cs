using AMLCore.Internal;
using AMLCore.Logging;
using AMLCore.Misc;
using Launcher.NativeEnums;
using Launcher.NativeStructs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Launcher
{
    static class Program
    {
        private static Logger _Logger;

        internal static void Run(string[] cliArgs)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Startup.Initialize(cliArgs);
            _Logger = new Logger("Launcher");
            _Logger.Info("launcher starts with arguments: {0}", String.Join(" ", cliArgs));

            RunInternal(cliArgs);

            WindowsHelper.StopThread();
            _Logger.Info("launcher exit");
        }

        private static void RunInternal(string[] cliArgs)
        {
            var args = LauncherArguments.Parse(cliArgs);

            if (args.RequiresGui)
            {
                bool dialogReturn = false;
                bool allowInjection = HasOnlineProcessStarted();
                WindowsHelper.RunAndWait(() => {
                    dialogReturn = args.ShowConfigDialog(allowInjection);
                });
                if (!dialogReturn)
                {
                    return;
                }
            }

            args.LogOptions();
            StartProcessWithArgs(args);
        }

        private static void Error(string msg, params string[] arg)
        {
            _Logger.Error(msg, arg);
        }

        private static bool HasOnlineProcessStarted()
        {
            var p = Process.GetProcessesByName("griefsyndrome_online");
            _Logger.Info("scan process 'griefsyndrome_online': {0} result(s)", p.Length);
            return p.Length == 1;
        }

        private static void StartProcessWithArgs(LauncherArguments args)
        {
            _Logger.Info("start new process");

            var pInfo = new PROCESS_INFORMATION();
            var pSec = new SECURITY_ATTRIBUTES();
            var tSec = new SECURITY_ATTRIBUTES();
            var sInfo = new STARTUPINFO();
            {
                pSec.nLength = Marshal.SizeOf(pSec);
                tSec.nLength = Marshal.SizeOf(tSec);

                if (!File.Exists(args.ProcessName))
                {
                    Error("process file not found: {0}", Path.GetFullPath(args.ProcessName));
                }
                bool retValue = Natives.CreateProcess(args.ProcessName, null,
                    ref pSec, ref tSec, false,
                    0x00000020 | 0x00000004, //NORMAL_PRIORITY_CLASS | CREATE_SUSPENDED
                    IntPtr.Zero, null, ref sInfo, out pInfo);

                Thread.Sleep(args.WaitLength);
                if (!retValue)
                {
                    Error("cannot start game, CreateProcess error 0x{0}",
                        Marshal.GetLastWin32Error().ToString("X8"));
                    return;
                }
            }

            //First call: LoadLibrary
            IntPtr injectedHandle;
            {
                var remoteAddr = WriteRemoteString(pInfo.hProcess, "aml/core/" + args.DllName);
                if (remoteAddr == IntPtr.Zero)
                {
                    return;
                }
                var pStart = Natives.GetProcAddress(Natives.GetModuleHandle("Kernel32"), "LoadLibraryW");
                _Logger.Info("LoadLibrary address 0x{0}", pStart.ToString("X8"));
                if (pStart == IntPtr.Zero)
                {
                    Error("cannot get LoadLibrary address");
                    return;
                }
                var hThread = Natives.CreateRemoteThread(pInfo.hProcess, IntPtr.Zero, 0,
                    pStart, remoteAddr, 0, out IntPtr lpThreadID);
                if (hThread == IntPtr.Zero)
                {
                    Error("cannot create remote thread 0x{0}",
                        Marshal.GetLastWin32Error().ToString("X8"));
                    return;
                }
                Natives.WaitForSingleObject(hThread, Natives.INFINITE);
                if (!Natives.GetExitCodeThread(hThread, out uint returnedValue))
                {
                    Error("cannot get library module handle, GetExitCodeThread error 0x{0}",
                        Marshal.GetLastWin32Error().ToString("X8"));
                }
                injectedHandle = (IntPtr)returnedValue;
                if (injectedHandle == IntPtr.Zero)
                {
                    Error("cannot load core library, LoadLibrary failed");
                    return;
                }
            }

            //Prepare for command-line arguments
            IntPtr dataPtr = WriteRemoteData(pInfo.hProcess, args.WriteInjectedData());
            if (dataPtr == IntPtr.Zero)
            {
                Error("cannot write argument data");
                return;
            }

            //Second call: GetProcAddress
            IntPtr loader;
            {
                var remoteAddrProcName = WriteRemoteData(pInfo.hProcess, StringToByteArrayANSI(args.ExportName));

                IntPtr[] ud = new IntPtr[3];
                ud[0] = Natives.GetProcAddress(Natives.GetModuleHandle("Kernel32"), "GetProcAddress");
                ud[1] = injectedHandle;
                ud[2] = remoteAddrProcName;
                var udBytes = new byte[12];
                Buffer.BlockCopy(ud, 0, udBytes, 0, 12);
                var remoteAddrUd = WriteRemoteData(pInfo.hProcess, udBytes);

                var remoteAddrFunc = WriteRemoteData(pInfo.hProcess, GenerateGetProcAddress(remoteAddrUd));

                var hThread = Natives.CreateRemoteThread(pInfo.hProcess, IntPtr.Zero, 0,
                    remoteAddrFunc, IntPtr.Zero, 0, out IntPtr lpThreadID);
                var ret = Natives.WaitForSingleObject(hThread, Natives.INFINITE);
                Natives.GetExitCodeThread(hThread, out uint returnedValue);
                loader = (IntPtr)returnedValue;
                if (loader == IntPtr.Zero)
                {
                    Error("cannot identify core entry, GetProcAddress failed");
                    return;
                }
            }

            //Third call: run loader
            {
                uint returnedValue;
                IntPtr lpThreadID;
                var hThread = Natives.CreateRemoteThread(pInfo.hProcess, IntPtr.Zero, 0,
                    loader, dataPtr, 0, out lpThreadID);
                var ret = Natives.WaitForSingleObject(hThread, Natives.INFINITE);
                Natives.GetExitCodeThread(hThread, out returnedValue);
                _Logger.Info("core loaded, exit code: 0x{0}", returnedValue.ToString("X8"));
            }

            Natives.ResumeThread(pInfo.hThread);

            if (args.WaitProcess)
            {
                _Logger.Info("waiting for the game process");
                Natives.WaitForSingleObject(pInfo.hProcess, Natives.INFINITE);
            }
            _Logger.Info("new process started");
        }

        private static byte[] StringToByteArray(string str)
        {
            var ret = new byte[str.Length * 2 + 2];
            Buffer.BlockCopy(str.ToCharArray(), 0, ret, 0, str.Length * 2);
            return ret;
        }

        private static byte[] StringToByteArrayANSI(string str)
        {
            var ret = new byte[str.Length + 1];
            for (int i = 0; i < str.Length; ++i)
            {
                ret[i] = (byte)str[i];
            }
            return ret;
        }

        private static IntPtr WriteRemoteString(IntPtr hProcess, string str)
        {
            var data = StringToByteArray(str);
            return WriteRemoteData(hProcess, data);
        }

        private static IntPtr WriteRemoteData(IntPtr hProcess, byte[] data)
        {
            var remoteAddr = Natives.VirtualAllocEx(hProcess, IntPtr.Zero, new IntPtr(data.Length),
                AllocationType.Commit, MemoryProtection.ExecuteReadWrite);
            if (remoteAddr == IntPtr.Zero)
            {
                Error("cannot write remote memory, VirtualAllocEx error 0x{0}",
                    Marshal.GetLastWin32Error().ToString("X8"));
                return IntPtr.Zero;
            }
            IntPtr lpNumberOfBytesWritten;
            var retWrite = Natives.WriteProcessMemory(hProcess, remoteAddr, data, data.Length, out lpNumberOfBytesWritten);
            if (!retWrite)
            {
                Error("cannot write remote memory, WriteProcessMemory error 0x{0}",
                    Marshal.GetLastWin32Error().ToString("X8"));
                return IntPtr.Zero;
            }
            return remoteAddr;
        }

        private static byte[] GenerateGetProcAddress(IntPtr lpRemoteData)
        {
            //generate a function like this (in C)
            /*
             * DWORD STDCALL ThreadStart()
             * {
             *      struct {
             *          void *(*f)(HMODULE hModule, LPCSTR lpProcName);
             *          HMODULE hModule;
             *          LPCSTR lpProcName;
             *      } *pData = ????????;
             *      return pData->f(pData->hModule, pData->lpProcName);
             *  }
             */
            //assembly
            /*
             * ; start
             * push ebp
             * mov ebp, esp
             * 
             * ; save used registers (esi, edi, ebx)
             * push esi
             * 
             * mov esi, ????????       ; pData
             * push dword ptr[esi + 8] ; lpProcName
             * push dword ptr[esi + 4] ; hModule
             * mov esi, ptr[esi]       ; f
             * call esi
             * 
             * ; restore registers
             * pop esi
             * 
             * ; finish
             * pop ebp
             * retn 4
             */
            byte[] assemblyCode =
            {
                0x55,
                0x89, 0xE5,
                0x56,
                0xBE, 0x00, 0x00, 0x00, 0x00,
                0xFF, 0x76, 0x08,
                0xFF, 0x76, 0x04,
                0x8B, 0xB6, 0x00, 0x00, 0x00, 0x00,
                0xFF, 0xD6,
                0x5E,
                0x5D,
                0xC2, 0x04, 0x00,
            };
            byte[] d = BitConverter.GetBytes(lpRemoteData.ToInt32());
            Array.Copy(d, 0, assemblyCode, 5, 4);
            return assemblyCode;
        }
    }
}
