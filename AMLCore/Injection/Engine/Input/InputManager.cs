using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AMLCore.Injection.Engine.Input
{
    public static class InputManager
    {
        private class InputHandlerInfo
        {
            public IInputHandler Handler;
            public bool ReceiveHandled;
        }

        private static List<InputHandlerInfo> _Hanlders = new List<InputHandlerInfo>();
        private static object _Mutex = new object();
        private static bool _RunFP;
        internal static volatile bool MainWindowDestroyed = false;

        public static bool Wait()
        {
            if (!MainWindowDestroyed)
            {
                Thread.Sleep(1);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool HandleAll(IntPtr ptr)
        {
            lock (_Mutex)
            {
                if (!_RunFP)
                {
                    _RunFP = true;
                    FloatingPointFlags.Reset();
                }
                if (KeyConfigRedirect.Redirected)
                {
                    KeyConfigRedirect.Preprocess(ptr);
                }
                bool handled = false;
                foreach (var h in _Hanlders)
                {
                    if (handled)
                    {
                        if (h.ReceiveHandled)
                        {
                            h.Handler.HandleInput(ptr);
                        }
                    }
                    else
                    {
                        handled = h.Handler.HandleInput(ptr);
                    }
                }
            }
            return false;
        }

        public static void RegisterHandler(IInputHandler h, bool receiveHandled)
        {
            lock (_Mutex)
            {
                _Hanlders.Add(new InputHandlerInfo { Handler = h, ReceiveHandled = receiveHandled });
            }
        }

        public static void RegisterHandler(IInputHandler h)
        {
            lock (_Mutex)
            {
                _Hanlders.Add(new InputHandlerInfo { Handler = h });
            }
        }

        public static void ZeroInputData(IntPtr ptr, int len)
        {
            Marshal.Copy(_Zero, 0, ptr, len > 0x100 ? 0x100 : len);
        }

        private static readonly byte[] _Zero = new byte[0x100];
    }
}
