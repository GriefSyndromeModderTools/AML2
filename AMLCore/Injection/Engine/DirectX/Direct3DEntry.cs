using AMLCore.Injection.Native;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.DirectX
{
    internal class Direct3DEntry : IEntryPointPreload
    {
        public void Run()
        {
            new InjectDirect3DCreate9();
        }

        private delegate IntPtr CreateD3D9Delegate(int version);
        private class InjectDirect3DCreate9 : FunctionPointerInjection<CreateD3D9Delegate>
        {
            public InjectDirect3DCreate9()
                : base(0x20E2D0)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var obj = Original(env.GetParameterI(0));
                new InjectD3DCreateDevice(obj);
                env.SetReturnValue(obj);
            }
        }

        private delegate int CreateDeviceDelegate(
            IntPtr pD3d,
            int adapter,
            int deviceType,
            IntPtr hWnd,
            int behaviorFlags,
            IntPtr pPresentParameters,
            IntPtr pResult);
        private class InjectD3DCreateDevice : FunctionPointerInjection<CreateDeviceDelegate>
        {
            public InjectD3DCreateDevice(IntPtr obj)
                : base(AddressHelper.VirtualTable(obj, 16))
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var pResult = env.GetParameterP(6);
                var ret = Original(
                    env.GetParameterP(0),
                    env.GetParameterI(1),
                    env.GetParameterI(2),
                    env.GetParameterP(3),
                    env.GetParameterI(4),
                    env.GetParameterP(5),
                    pResult);
                env.SetReturnValue((IntPtr)ret);
                Direct3DHelper.OnDeviceCreated(Marshal.ReadIntPtr(pResult));
            }
        }
    }
}
