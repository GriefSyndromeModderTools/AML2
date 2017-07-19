using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Internal
{
    internal class TestInjection : Injection.Native.FunctionPointerInjection<TestInjection.CreateDeviceDelegate>
    {
        internal delegate IntPtr CreateDeviceDelegate(int version);

        public TestInjection()
            : base(0x20E2D0)
        {
        }

        protected override void Triggered(NativeEnvironment env)
        {
            InvokeOriginal(env);
        }
    }

    internal class TestEntry : IEntryPointLoad
    {
        public void Run()
        {
            //new TestInjection();
            //AMLCore.Injection.Engine.Input.KeyConfigRedirect.Redirect();
        }
    }
}
