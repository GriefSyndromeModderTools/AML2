using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AMLCore.Injection.Engine.Input
{
    public enum InputHandlerType
    {
        RawInput,
        Modify,
        GameInput,
    }

    public static class InputManager
    {
        private class InputHandlerInfo
        {
            public IInputHandler Handler;
            public InputHandlerType Type;
        }

        private static IInputHandler _ModifyHandler = null;
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
                bool handled = _ModifyHandler?.HandleInput(ptr) ?? false;
                foreach (var h in _Hanlders)
                {
                    if (handled)
                    {
                        if (h.Type != InputHandlerType.RawInput)
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

        public static void RegisterHandler(IInputHandler h, InputHandlerType type)
        {
            lock (_Mutex)
            {
                if (type == InputHandlerType.Modify)
                {
                    if (_ModifyHandler != null)
                    {
                        throw new InvalidOperationException("Another modify handler is already registered.");
                    }
                    _ModifyHandler = h;
                }
                else
                {
                    _Hanlders.Add(new InputHandlerInfo { Handler = h, Type = type });
                }
            }
        }

        public static void ZeroInputData(IntPtr ptr, int len)
        {
            Marshal.Copy(_Zero, 0, ptr, len > 0x100 ? 0x100 : len);
        }

        private static readonly byte[] _Zero = new byte[0x100];
    }
}
