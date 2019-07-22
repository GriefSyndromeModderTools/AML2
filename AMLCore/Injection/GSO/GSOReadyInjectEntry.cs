using AMLCore.Injection.Native;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.GSO
{
    internal class GSOReadyInjectEntry : IEntryPointLoad
    {
        public void Run()
        {
            new GSOReady();
        }

        private class GSOReady : CodeInjection
        {
            public GSOReady() : base(0x1AB336, 7)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                PostGSOInjection.Invoke();
            }
        }
    }
}
