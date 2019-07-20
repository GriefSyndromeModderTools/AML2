using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Engine.Window
{
    public delegate void WindowProc(IntPtr hWnd, uint uMsg, int wParam, int lParam);

    public static class MainWindowHelper
    {
        private static readonly List<WindowProc> _ProcList = new List<WindowProc>();

        public static void RegisterMessageHandler(WindowProc proc)
        {
            _ProcList.Add(proc);
        }

        internal static void Invoke(IntPtr hWnd, uint uMsg, int wParam, int lParam)
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
    }
}
