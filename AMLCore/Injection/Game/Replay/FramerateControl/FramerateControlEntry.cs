using AMLCore.Injection.Engine.DirectX;
using AMLCore.Injection.Engine.Renderer;
using AMLCore.Injection.Game.Scene;
using AMLCore.Injection.Native;
using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Replay.FramerateControl
{
    class FramerateControlEntry : IEntryPointLoad, ISceneEventHandler
    {
        public static bool Enabled = false;

        public void Run()
        {
            Direct3DHelper.InjectDevice(d =>
            {
                var showFps = new IniFile("Core").Read("Misc", "ShowFps", "0") != "0";
                if (showFps) CodeModification.Modify(0x2AC11E, 1);

                if (Enabled) WindowsHelper.Run(() => new GuiController().Show());
            });

            SceneInjectionManager.RegisterSceneHandler(SystemScene.StageMain, this);
        }

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
            if (FramerateHelper.Skip < 1.0f)
            {
                FramerateHelper.Skip += (FramerateHelper.Ratio - 1);
            }
            else
            {
                FramerateHelper.Skip -= 1.0f;
                SkipRenderer.SkipOnce();
            }
        }
    }
}
