﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Internal
{
    internal class Natives
    {
        [DllImport("kernel32.dll", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, int count);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize,
           Protection flNewProtect, out Protection lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAlloc(IntPtr lpAddress, IntPtr dwSize,
           AllocationType flAllocationType, Protection flProtect);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        public static extern void GetCurrentThreadStackLimits(out IntPtr low, out IntPtr high);

        [DllImport("user32.dll", EntryPoint = "CreateWindowExW")]
        public static extern IntPtr CreateWindowEx(int dwExStyle,
            [In, MarshalAs(UnmanagedType.LPWStr)] string className,
            [In, MarshalAs(UnmanagedType.LPWStr)] string windowName,
            int dwStyle, int x, int y, int w, int h, IntPtr hParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lParam);

        [DllImport("user32.dll", EntryPoint = "PostMessageW")]
        public static extern IntPtr PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        
        [DllImport("ws2_32.dll", EntryPoint = "sendto")]
        public static extern int SendTo(IntPtr Socket, IntPtr buff, int len, int flags, ref ulong addr, int addrLen);

        [DllImport("ws2_32.dll", EntryPoint = "sendto")]
        public static extern int SendTo(IntPtr Socket, ref byte buff, int len, int flags, ref ulong addr, int addrLen);

        [DllImport("ws2_32.dll", EntryPoint = "recvfrom")]
        public static extern int RecvFrom(IntPtr Socket, IntPtr buff, int len, int flags, ref ulong addr, ref int addrLen);

        [DllImport("kernel32.dll")]
        public static extern void EnterCriticalSection(IntPtr lpCriticalSection);

        [DllImport("kernel32.dll")]
        public static extern void LeaveCriticalSection(IntPtr lpCriticalSection);

        [Flags]
        public enum AllocationType : uint
        {
            COMMIT = 0x1000,
            RESERVE = 0x2000,
            RESET = 0x80000,
            LARGE_PAGES = 0x20000000,
            PHYSICAL = 0x400000,
            TOP_DOWN = 0x100000,
            WRITE_WATCH = 0x200000
        }

        [Flags]
        public enum Protection
        {
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400
        }
    }
}
