using AMLCore.Injection.Engine.Script;
using AMLCore.Injection.Native;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene
{
    internal class SceneInjectEntry : IEntryPointPreload
    {
        public void Run()
        {
            foreach (var s in SceneInjectionManager.SystemSceneNames)
            {
                InjectSingle(s.Value, s.Key);
            }
            new EndStageInjection();
        }

        private void InjectSingle(string name, SystemScene scene)
        {
            var func1 = SquirrelHelper.InjectCompileFile($"data/system/{name}/{name}.global.nut", "Update");
            func1.AddBefore(CreateCallback(SceneInjectionManager.PreUpdateCallback, scene));
            func1.AddAfter(CreateCallback(SceneInjectionManager.PostUpdateCallback, scene));
            var func2 = SquirrelHelper.InjectCompileFile($"data/system/{name}/{name}.global.nut", "Init");
            func2.AddAfter(CreateCallback(SceneInjectionManager.PostInitCallback, scene));
        }

        private InjectedScriptDelegate CreateCallback(Action<SystemScene> inner, SystemScene value)
        {
            return delegate (IntPtr vm)
            {
                inner(value);
            };
        }

        private class EndStageInjection : CodeInjection
        {
            public EndStageInjection() : base(0x1102E1, 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                SceneInjectionManager.EndStageCallback();
            }
        }
    }
}
