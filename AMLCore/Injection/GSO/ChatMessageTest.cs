using AMLCore.Injection.Engine.Window;
using AMLCore.Injection.Game.Scene;
using AMLCore.Injection.Native;
using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.GSO
{
    class ChatMessageTest : IEntryPointLoad, ISceneEventHandler
    {
        private delegate void ShowChatMessageDelegate(int player, [MarshalAs(UnmanagedType.LPWStr), In] string msg);
        private static ShowChatMessageDelegate ShowChatMessage;

        public void Exit()
        {
        }

        public void PostInit(SceneEnvironment env)
        {
            MainWindowHelper.Invoke(() =>
            {
                //ShowChatMessage(0, "Hello world!");
            });
        }

        public void PostUpdate()
        {
        }

        public void PreUpdate()
        {
        }

        public void Run()
        {
            if (!PostGSOInjection.IsGSO) return;
            SceneInjectionManager.RegisterSceneHandler(SystemScene.Title, this);
            PostGSOInjection.Run(() =>
            {
                ShowChatMessage = (ShowChatMessageDelegate)Marshal.GetDelegateForFunctionPointer(AddressHelper.Code("gso", 0x2BE0),
                    typeof(ShowChatMessageDelegate));
            });
        }
    }
}
