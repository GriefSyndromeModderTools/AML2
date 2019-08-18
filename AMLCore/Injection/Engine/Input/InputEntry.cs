using AMLCore.Injection.GSO;
using AMLCore.Injection.Native;
using AMLCore.Logging;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.Input
{
    internal class InputEntry : IEntryPointLoad
    {
        public void Run()
        {
            CodeModification.FillNop(0xC5FF8, 5);
            if (PostGSOInjection.IsGSO)
            {
                PostGSOInjection.Run(() =>
                {
                    new GSOInputInjection();
                });
            }
            else
            {
                KeyConfigRedirect.Inject();
                new InjectCoCreateInstance();
            }
        }

        private class GSOInputInjection : CodeInjection
        {
            private IntPtr status = AddressHelper.Code("gso", 0x28900);

            public GSOInputInjection() : base(0xC6000, 7)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                IntPtr data = AddressHelper.Code(0x2AB6F0);
                if (Marshal.ReadInt32(status) != 4)
                {
                    InputManager.HandleAll(data);
                }
            }
        }

        private delegate int CoCreateInstanceDelegate(
            IntPtr rclsid, IntPtr pUnkOuter, int dwClsContext, IntPtr riid, IntPtr ppv);
        private class InjectCoCreateInstance : FunctionPointerInjection<CoCreateInstanceDelegate>
        {
            public InjectCoCreateInstance()
                : base(0x20E37C)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                InvokeOriginal(env);
                if (env.GetParameterI(2) == 0x17)
                {
                    new InjectCreateDevice(Marshal.ReadIntPtr(env.GetParameterP(4)));
                }
            }
        }

        private delegate int CreateDeviceDelegate(IntPtr p0, IntPtr p1, IntPtr p2, IntPtr p3);
        private class InjectCreateDevice : FunctionPointerInjection<CreateDeviceDelegate>
        {
            private static bool _Injected = false;

            public InjectCreateDevice(IntPtr obj)
                : base(AddressHelper.VirtualTable(obj, 3))
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var p0 = env.GetParameterP(0);
                var p1 = env.GetParameterP(1);
                var p2 = env.GetParameterP(2);
                var p3 = env.GetParameterP(3);

                var ret = Original(p0, p1, p2, p3);
                env.SetReturnValue(ret);

                if (!_Injected)
                {
                    _Injected = true;
                    new InjectGetDeviceState(Marshal.ReadIntPtr(p2));
                }
            }
        }
        
        private delegate int GetDeviceStateDelegate(IntPtr p0, int p1, IntPtr p2);
        private class InjectGetDeviceState : FunctionPointerInjection<GetDeviceStateDelegate>
        {
            private static IntPtr _InjectedInstance;

            public InjectGetDeviceState(IntPtr co)
                : base(AddressHelper.VirtualTable(co, 9))
            {
                _InjectedInstance = co;
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var p0 = env.GetParameterP(0);
                var p1 = env.GetParameterI(1);
                var p2 = env.GetParameterP(2);

                InputManager.ZeroInputData(p2, p1);
                var ret = Original(p0, p1, p2);

                if (p0 == _InjectedInstance)
                {
                    InputManager.HandleAll(p2);
                }
                env.SetReturnValue(ret);
            }
        }
    }
}
