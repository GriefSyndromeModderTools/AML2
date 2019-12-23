using AMLCore.Injection.Engine.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.StagePause
{
    internal class StagePauseHandler : ISceneEventHandler
    {
        public static Dictionary<int, ICharacterStagePauseHandler> _handlers = new Dictionary<int, ICharacterStagePauseHandler>();
        private SceneEnvironment _env;
        private bool[] _textVisible = new bool[3];

        public void PostInit(SceneEnvironment env)
        {
            _env = env;
        }

        public void PreUpdate()
        {
            DrawPlayer(_env, 0);
            DrawPlayer(_env, 1);
            DrawPlayer(_env, 2);
        }

        public void PostUpdate()
        {
            if (_textVisible[0]) _env.GetElement("status_1p").Visible = true;
            if (_textVisible[1]) _env.GetElement("status_2p").Visible = true;
            if (_textVisible[2]) _env.GetElement("status_3p").Visible = true;
        }

        public void Exit()
        {
        }

        private void DrawPlayer(SceneEnvironment env, int i)
        {
            var vm = SquirrelHelper.SquirrelVM;
            _textVisible[i] = false;

            using (var player = SquirrelHelper.PushMemberChainRoot("actor", "player" + (i + 1).ToString()))
            {
                if (!player.IsSuccess) return;
                var type = SquirrelHelper.GetInt32("type");
                if (type > 6 && _handlers.TryGetValue(type, out var handler))
                {
                    //stack: actor table, player

                    int left = 310 + 160 * i;
                    int top = 511;
                    _textVisible[i] = true;

                    handler.DrawName(env, left, top);
                    env.DrawNumber("status_num", left + 65, top + 20, SquirrelHelper.GetInt32("level"), -2, 1);
                    env.DrawNumber("status_num", left + 77, top + 42, SquirrelHelper.GetInt32("soulMax"), -5, 1);
                    env.DrawNumber("status_num", left + 31, top + 63, SquirrelHelper.GetInt32("lifeMax"), -2, 1);
                    env.DrawNumber("status_num", left + 108, top + 63, SquirrelHelper.GetInt32("baseAtk"), -2, 1);

                    var frame = env.GetElement("frame_p" + (i + 1).ToString());
                    handler.DrawImage(env, i, (int)frame.DestX, (int)frame.DestY);

                    var img = env.GetResource($"c{i + 1}pP");
                    env.BitBlt(img, frame.DestX + 57, frame.DestY + 345, img.ImageWidth, img.ImageHeight, 0, 0, Blend.Alpha, 1);
                }
            }
        }
    }
}
