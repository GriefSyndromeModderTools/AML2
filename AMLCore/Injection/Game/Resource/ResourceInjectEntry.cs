using AMLCore.Injection.Native;
using AMLCore.Internal;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AMLCore.Injection.Game.Resource
{
    public class ResourceInjectEntry : IEntryPointLoad
    {
        public void Run()
        {
            new InjectBefore();
            new InjectAfter();
        }

        private static ThreadLocal<int> _lastId = new ThreadLocal<int>(() => -1);

        private class InjectBefore : CodeInjection
        {
            public InjectBefore()
                : base(0xD0C63, 7)
            {
                AddRegisterRead(Register.ECX);
                AddRegisterRead(Register.EBP);
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var str = Marshal.PtrToStringAnsi(env.GetRegister(Register.ECX));
                CoreLoggers.Injection.Info("Resource " + str);
                var id = ResourceInjection.GetResourceId(str);
                _lastId.Value = id;
            }
        }

        private class InjectAfter : CodeInjection
        {
            public InjectAfter()
                : base(0xD0CCA, 6)
            {
                AddRegisterRead(Register.ECX);
                AddRegisterRead(Register.EBP);
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var id = _lastId.Value;
                var obj = env.GetParameterP(0);
                if (id != -1)
                {
                    ResourceObject.CloseOriginal(obj);
                    ResourceObject.Init(obj, id, ResourceInjection.GetResource(id).Length);
                    CoreLoggers.Injection.Info("Resource replaced");
                }
            }
        }
    }
}
