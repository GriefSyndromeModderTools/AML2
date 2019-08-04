using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.StageSelect
{
    public interface ICharacterPictureRenderer
    {
        void DrawSelected(SceneEnvironment env, float x, float y, int sliceX, int sliceWidth, float alpha);
        void DrawDead(SceneEnvironment env, float x, float y, int sliceX, int sliceWidth, float alpha);
    }

    internal class EmptyCharacterPictureRenderer : ICharacterPictureRenderer
    {
        public static readonly EmptyCharacterPictureRenderer Instance = new EmptyCharacterPictureRenderer();

        public void DrawDead(SceneEnvironment env, float x, float y, int sliceX, int sliceWidth, float alpha)
        {
        }

        public void DrawSelected(SceneEnvironment env, float x, float y, int sliceX, int sliceWidth, float alpha)
        {
        }
    }

    internal class VanillaCharacterPictureRenderer : ICharacterPictureRenderer
    {
        private readonly string _resNameB, _resNameC;

        public VanillaCharacterPictureRenderer(string resName)
        {
            _resNameB = resName + "_B";
            _resNameC = resName + "_C";
        }

        public void DrawDead(SceneEnvironment env, float x, float y, int sliceX, int sliceWidth, float alpha)
        {
            var img = env.GetResource(_resNameC);
            env.BitBlt(img, x + sliceX, y, sliceWidth, img.ImageHeight, sliceX, 0, Blend.Alpha, alpha);
        }

        public void DrawSelected(SceneEnvironment env, float x, float y, int sliceX, int sliceWidth, float alpha)
        {
            var img = env.GetResource(_resNameB);
            env.BitBlt(img, x + sliceX, y, sliceWidth, img.ImageHeight, sliceX, 0, Blend.Alpha, alpha);
        }
    }

    public class ResourceCharacterPictureRenderer : ICharacterPictureRenderer
    {
        private readonly string _sel, _dead;

        public ResourceCharacterPictureRenderer(string sel, string dead)
        {
            _sel = sel;
            _dead = dead;
        }

        public void DrawDead(SceneEnvironment env, float x, float y, int sliceX, int sliceWidth, float alpha)
        {
            var img = env.CreateResource(_dead);
            env.BitBlt(img, x + sliceX, y, sliceWidth, img.ImageHeight, sliceX, 0, Blend.Alpha, alpha);
        }

        public void DrawSelected(SceneEnvironment env, float x, float y, int sliceX, int sliceWidth, float alpha)
        {
            var img = env.CreateResource(_sel);
            env.BitBlt(img, x + sliceX, y, sliceWidth, img.ImageHeight, sliceX, 0, Blend.Alpha, alpha);
        }
    }
}
