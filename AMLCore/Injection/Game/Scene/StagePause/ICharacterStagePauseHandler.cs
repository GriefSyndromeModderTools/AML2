using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.StagePause
{
    public interface ICharacterStagePauseHandler
    {
        void DrawName(SceneEnvironment env, int x, int y);
        void DrawImage(SceneEnvironment env, int playerIndex, int x, int y);
    }

    public class ResourceStagePauseHandler : ICharacterStagePauseHandler
    {
        private readonly string _name, _img;

        public ResourceStagePauseHandler(string name, string img)
        {
            _name = name;
            _img = img;
        }

        public void DrawImage(SceneEnvironment env, int playerIndex, int x, int y)
        {
            var r = env.CreateResource(_img);
            env.BitBlt(r, x, y, r.ImageWidth, r.ImageHeight, 0, 0, Blend.Alpha, 1);
        }

        public void DrawName(SceneEnvironment env, int x, int y)
        {
            var r = env.CreateResource(_name);
            env.BitBlt(r, x, y, r.ImageWidth, r.ImageHeight, 0, 0, Blend.Alpha, 1);
        }
    }
}
