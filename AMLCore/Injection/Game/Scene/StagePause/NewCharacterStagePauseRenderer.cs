using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.StagePause
{
    public class NewCharacterStagePauseRenderer
    {
        private static bool _enabled;

        private static void Enable()
        {
            if (!_enabled)
            {
                _enabled = true;
                SceneInjectionManager.RegisterSceneHandler(SystemScene.StagePause, new StagePauseHandler());
            }
        }

        public static void EnableCharacter(int type, ICharacterStagePauseHandler handler)
        {
            Enable();
            StagePauseHandler._handlers.Add(type, handler);
        }
    }
}
