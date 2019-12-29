using AMLCore.Injection.Engine.DirectX.ActorTransform;
using AMLCore.Injection.Engine.Script;
using AMLCore.Injection.Game.Scene;
using AMLCore.Injection.Native;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.GSO
{
    class GSOChatMessageFix : IEntryPointLoad, ISceneEventHandler
    {
        private static IntPtr p1 = IntPtr.Zero, p2 = IntPtr.Zero, p3 = IntPtr.Zero;

        public void Exit()
        {
        }

        public void PostInit(SceneEnvironment env)
        {
        }

        public void PostUpdate()
        {
        }

        public void PreUpdate()
        {
            using (SquirrelHelper.PushMemberChainThis("guage"))
            {
                var gsize = SquirrelFunctions.getsize(SquirrelHelper.SquirrelVM, -1);
                if (gsize <= 0)
                {
                    return;
                }
            }
            using (var player = SquirrelHelper.PushMemberChainRoot("actor", "player1"))
            {
                if (player.IsSuccess)
                {
                    SquirrelFunctions.getinstanceup(SquirrelHelper.SquirrelVM, -1, out var ptr, IntPtr.Zero);
                    p1 = ptr;
                }
                else
                {
                    p1 = IntPtr.Zero;
                }
            }
            using (var player = SquirrelHelper.PushMemberChainRoot("actor", "player2"))
            {
                if (player.IsSuccess)
                {
                    SquirrelFunctions.getinstanceup(SquirrelHelper.SquirrelVM, -1, out var ptr, IntPtr.Zero);
                    p2 = ptr;
                }
                else
                {
                    p2 = IntPtr.Zero;
                }
            }
            using (var player = SquirrelHelper.PushMemberChainRoot("actor", "player3"))
            {
                if (player.IsSuccess)
                {
                    SquirrelFunctions.getinstanceup(SquirrelHelper.SquirrelVM, -1, out var ptr, IntPtr.Zero);
                    p3 = ptr;
                }
                else
                {
                    p3 = IntPtr.Zero;
                }
            }
        }

        public void Run()
        {
            if (!PostGSOInjection.IsGSO) return;
            PostGSOInjection.Run(() =>
            {
                new FixPlayerId();
            });
            SceneInjectionManager.RegisterSceneHandler(SystemScene.StageMain, this);
        }

        private class FixPlayerId : CodeInjection
        {
            private IntPtr _playerIdAddr;

            public FixPlayerId() : base(AddressHelper.Code("gso", 0x5FA3), 6)
            {
                _playerIdAddr = AddressHelper.Code("gso", 0x268B0);
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var playerPtr = ActorTransformEntry.CurrentActorObj;
                if (playerPtr == p1)
                {
                    Marshal.WriteInt32(_playerIdAddr, 0);
                }
                else if (playerPtr == p2)
                {
                    Marshal.WriteInt32(_playerIdAddr, 1);
                }
                else if (playerPtr == p3)
                {
                    Marshal.WriteInt32(_playerIdAddr, 2);
                }
                else
                {
                    Marshal.WriteInt32(_playerIdAddr, -1);
                }
            }
        }
    }
}
