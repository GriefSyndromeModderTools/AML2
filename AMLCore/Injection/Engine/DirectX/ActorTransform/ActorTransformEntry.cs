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
        //Also used by GSO.GSOChatMessageFix
        internal static IntPtr CurrentActorObj;

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
                    CurrentActorObj = actorPtr;
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
                if (CurrentActorObj != IntPtr.Zero)
                {
                    var matPtr = env.GetParameterP(0);
                    ActorTransformManager.BeforeDrawActor(CurrentActorObj, matPtr);
                    CurrentActorObj = IntPtr.Zero;
                }
            }
        }
    }
}
