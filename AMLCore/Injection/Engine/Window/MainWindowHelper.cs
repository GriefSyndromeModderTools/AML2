using AMLCore.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Engine.Window
{
    public delegate void WindowProc(IntPtr hWnd, uint uMsg, int wParam, int lParam);

    public static class MainWindowHelper
    {
        private static readonly List<WindowProc> _ProcList = new List<WindowProc>();
        private static readonly ConcurrentQueue<Action> _Actions = new ConcurrentQueue<Action>();

        public static void RegisterMessageHandler(WindowProc proc)
        {
            _ProcList.Add(proc);
        }

        internal static void RunWinProcHandlers(IntPtr hWnd, uint uMsg, int wParam, int lParam)
        {
            foreach (var p in _ProcList)
            {
                try
                {
                    p(hWnd, uMsg, wParam, lParam);
                }
                catch (Exception e)
                {
                    CoreLoggers.Main.Error("exception in WindProc: {0}", e.ToString());
                }
            }
        }

        internal static void ExecuteLoop()
        {
            while (_Actions.TryDequeue(out var aa))
            {
                aa();
            }
        }

        public static void Invoke(Action action)
        {
            _Actions.Enqueue(action);
        }
    }
}
