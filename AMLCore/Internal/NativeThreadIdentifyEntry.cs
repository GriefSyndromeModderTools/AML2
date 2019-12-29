using AMLCore.Injection.Native;
using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Internal
{
    internal class NativeThreadIdentifyEntry
    {
        public static void Run()
        {
            new InitThreadEntryInjection();
            new MainThreadEntryInjection();
        }

        private class InitThreadEntryInjection : CodeInjection
        {
            public InitThreadEntryInjection() : base(0x1B8C6B, 8)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                ThreadHelper.InitInternalThread("Init");
            }
        }

        private class MainThreadEntryInjection : CodeInjection
        {
            public MainThreadEntryInjection() : base(0xC5EC0, 9)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                ThreadHelper.InitInternalThread("Main");
            }
        }
    }
}
