using AMLCore.Injection.Native;
using AMLCore.Internal;
using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Engine.Window
{
    internal class MainWindowMessageEntry : IEntryPointLoad
    {
        public void Run()
        {
            new MainWindowMessageInjection();
            MainWindowHelper.RegisterMessageHandler(CheckWindowsClosed);
        }

        private static void CheckWindowsClosed(IntPtr hWnd, uint uMsg, int wParam, int lParam)
        {
            if (uMsg == 2 /* WM_DESTROY */)
            {
                CoreLoggers.Main.Info("main window destroyed");
                ThreadHelper.TerminateAllThreads();
            }
        }

        private class MainWindowMessageInjection : CodeInjection
        {
            public MainWindowMessageInjection() : base(0x9FA93, 7)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                MainWindowHelper.Invoke(env.GetParameterP(0), (uint)env.GetParameterI(1),
                    env.GetParameterI(2), env.GetParameterI(3));
            }
        }
    }
}
