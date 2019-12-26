using AMLCore.Injection.Native;
using AMLCore.Internal;
using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.Window
{
    internal class MainWindowMessageEntry : IEntryPointLoad
    {
        [DllImport("Imm32.dll")]
        private static extern IntPtr ImmAssociateContext(IntPtr wnd, IntPtr imc);

        public void Run()
        {
            new MainWindowMessageInjection();
            new MainWindowMessageLoopInjection();
            MainWindowHelper.RegisterMessageHandler(CheckWindowsClosed);
            MainWindowHelper.RegisterMessageHandler(DisableIME);
        }

        private static void DisableIME(IntPtr hWnd, uint uMsg, int wParam, int lParam)
        {
            if (uMsg == 1 /* WM_CREATE */)
            {
                ImmAssociateContext(hWnd, IntPtr.Zero);
            }
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
                MainWindowHelper.RunWinProcHandlers(env.GetParameterP(0), (uint)env.GetParameterI(1),
                    env.GetParameterI(2), env.GetParameterI(3));
            }
        }

        private class MainWindowMessageLoopInjection : CodeInjection
        {
            public MainWindowMessageLoopInjection() : base(0xC5E40, 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                MainWindowHelper.ExecuteLoop();
            }
        }
    }
}
