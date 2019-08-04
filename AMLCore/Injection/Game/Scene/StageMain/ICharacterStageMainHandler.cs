using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.StageMain
{
    public interface ICharacterStageMainHandler
    {
        Resource GetSoulGem(SceneEnvironment env);
        void DrawName(SceneEnvironment env, int x, int y);
        void DrawSmallFace(SceneEnvironment env, int x, int y);
    }

    public class ResourceCharacterStageMainHandler : ICharacterStageMainHandler
    {
        private readonly string _name, _sg, _face;

        public ResourceCharacterStageMainHandler(string name, string sg, string face)
        {
            _name = name;
            _sg = sg;
            _face = face;
        }

        public void DrawName(SceneEnvironment env, int x, int y)
        {
            var r = env.CreateResource(_name);
            env.BitBlt(r, x, y, r.ImageWidth, r.ImageHeight, 0, 0, Blend.Alpha, 1);
        }

        public void DrawSmallFace(SceneEnvironment env, int x, int y)
        {
            var r = env.CreateResource(_face);
            env.BitBlt(r, x, y, 16, 16, 0, 0, Blend.Alpha, 1);
        }

        public Resource GetSoulGem(SceneEnvironment env)
        {
            return env.CreateResource(_sg);
        }
    }
}
