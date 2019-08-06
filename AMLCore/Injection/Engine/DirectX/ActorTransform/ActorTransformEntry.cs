using AMLCore.Injection.Native;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Engine.DirectX.ActorTransform
{
    internal class ActorTransformEntry : IEntryPointLoad
    {
        private static IntPtr _currentActorObj;

        public void Run()
        {
            new IterateActor();
            new DrawActor();
        }

        private class IterateActor : CodeInjection
        {
            public IterateActor() : base(0x7D05, 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var actorPtr = env.GetRegister(Register.EAX);
                var actorObj = new ActorObject(actorPtr);
                if (actorObj.IsActive && actorObj.AnimationInfoAvailable)
                {
                    _currentActorObj = actorPtr;
                }
            }
        }

        private class DrawActor : CodeInjection
        {
            public DrawActor() : base(0xB63A2, 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                if (_currentActorObj != IntPtr.Zero)
                {
                    var matPtr = env.GetParameterP(0);
                    ActorTransformManager.BeforeDrawActor(_currentActorObj, matPtr);
                    _currentActorObj = IntPtr.Zero;
                }
            }
        }
    }
}
