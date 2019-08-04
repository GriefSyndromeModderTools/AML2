using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.StageMain
{
    public static class NewCharacterStageMainRenderer
    {
        private static bool _enabled;

        private static void Enable()
        {
            if (!_enabled)
            {
                _enabled = true;
                SceneInjectionManager.RegisterSceneHandler(SystemScene.StageMain, new StageMainHandler());
            }
        }

        public static void EnableCharacter(int type, ICharacterStageMainHandler handler)
        {
            Enable();
            StageMainHandler._handlers.Add(type, handler);
        }
    }
}
