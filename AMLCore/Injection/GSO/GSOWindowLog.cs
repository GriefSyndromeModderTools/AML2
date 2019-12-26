using AMLCore.Injection.Native;
using AMLCore.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.GSO
{
    public class GSOWindowLog
    {
        private static ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();

        private static IntPtr GetTextCtrlHandle()
        {
            return Marshal.ReadIntPtr(AddressHelper.Code("gso", 0x286AC));
        }

        public static void WriteLine(string str)
        {
            _queue.Enqueue(str + "\r\n");
        }

        public static void WriteLine(string str, params string[] obj)
        {
            WriteLine(string.Format(str, obj));
        }

        private static void PrintQueue()
        {
            var hwnd = GetTextCtrlHandle();
            while (_queue.TryDequeue(out var str))
            {
                var len = Natives.GetWindowTextLength(hwnd);
                Natives.SendMessage(hwnd, 0xB1, len, len);
                Natives.SendMessage(hwnd, 0xC2, 0, str);
            }
        }

        internal static void Inject()
        {
            new GSOConnectDialogLoop();
        }

        private class GSOConnectDialogLoop : CodeInjection
        {
            public GSOConnectDialogLoop() : base(AddressHelper.Code("gso", 0x2782), 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                PrintQueue();
            }
        }
    }
}
