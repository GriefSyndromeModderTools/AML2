using AMLCore.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AMLCore.Misc
{
    public class WindowsHelper
    {
        private static Thread _WindowsThread;
        private static ConcurrentQueue<Action> _Queue = new ConcurrentQueue<Action>();

        static WindowsHelper()
        {
            if (Startup.Mode == StartupMode.Injected)
            {
                _WindowsThread = ThreadHelper.StartThread(WindowsThreadStart);
            }
        }

        //run cctor before any windows starts to solve "invalid window class name"
        public static void Init()
        {
        }

        public static void MessageBox(string text)
        {
            System.Windows.Forms.MessageBox.Show(text, "AML");
        }

        private static volatile bool _Stopped;
        
        [STAThread]
        [AMLThread(Name = "WinMsg")]
        private static void WindowsThreadStart()
        {
            if (Startup.Mode != StartupMode.Injected)
            {
                CoreLoggers.Main.Error("internal error: running windows thread in launcher");
            }
            CoreLoggers.Main.Info("windows thread starts");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            _Stopped = false;
            while (!_Stopped)
            {
                try
                {
                    while (_Queue.TryDequeue(out var a))
                    {
                        DoEvents();
                        a();
                    }
                    Thread.Sleep(10);
                    DoEvents();
                }
                catch (Exception e)
                {
                    CoreLoggers.Main.Error("exception in windows callback: {0}", e.ToString());
                }
            }
            CoreLoggers.Main.Info("windows thread exits");
        }

        public static void StopThread()
        {
            if (_WindowsThread == null)
            {
                return;
            }
            _Stopped = true;
            if (Thread.CurrentThread == _WindowsThread)
            {
                throw new Exception("abort thread");
            }
            else
            {
                Thread.Sleep(250);
                if (_WindowsThread.IsAlive)
                {
                    CoreLoggers.Main.Info("abort windows thread on demand");
                    _WindowsThread.Abort();
                }
            }
        }

        public static void Run(Action callback)
        {
            if (_WindowsThread == null)
            {
                callback();
                return;
            }

            if (Thread.CurrentThread == _WindowsThread)
            {
                callback();
            }
            _Queue.Enqueue(callback);
        }

        public static void RunAndWait(Action a)
        {
            if (_WindowsThread == null)
            {
                a();
                return;
            }

            var wait = new Wait { Original = a };
            Run(wait.Run);
            while (!wait.Finished)
            {
                Thread.Sleep(5);
            }
        }

        private class Wait
        {
            public volatile bool Finished;
            public Action Original;

            public void Run()
            {
                Original();
                Finished = true;
            }
        }

        //use the same method as SharpDX

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr handle;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
        }

        [DllImport("user32.dll", EntryPoint = "PeekMessage"), SuppressUnmanagedCodeSecurity]
        public static extern int PeekMessage(out NativeMessage lpMsg, IntPtr hWnd, int wMsgFilterMin,
                                              int wMsgFilterMax, int wRemoveMsg);

        [DllImport("user32.dll", EntryPoint = "GetMessage"), SuppressUnmanagedCodeSecurity]
        public static extern int GetMessage(out NativeMessage lpMsg, IntPtr hWnd, int wMsgFilterMin,
                                             int wMsgFilterMax);

        [DllImport("user32.dll", EntryPoint = "TranslateMessage"), SuppressUnmanagedCodeSecurity]
        public static extern int TranslateMessage(ref NativeMessage lpMsg);

        [DllImport("user32.dll", EntryPoint = "DispatchMessage"), SuppressUnmanagedCodeSecurity]
        public static extern int DispatchMessage(ref NativeMessage lpMsg);

        private static void DoEvents()
        {
            NativeMessage msg;
            while (PeekMessage(out msg, IntPtr.Zero, 0, 0, 0) != 0)
            {
                if (GetMessage(out msg, IntPtr.Zero, 0, 0) == -1)
                {
                    return;
                }

                var message = new Message() {
                    HWnd = msg.handle, LParam = msg.lParam,
                    Msg = (int)msg.msg, WParam = msg.wParam
                };
                if (!Application.FilterMessage(ref message))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
            }
        }
    }
}
