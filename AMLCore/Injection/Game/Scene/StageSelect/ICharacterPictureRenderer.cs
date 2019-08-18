using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.StageSelect
{
    public interface ICharacterPictureRenderer
    {
        void DrawSelected(ICharacterSelectionDataProvider env, int playerId, int sliceX, int sliceWidth);
        void DrawDead(ICharacterSelectionDataProvider env, int playerId, int sliceX, int sliceWidth);
    }

    internal class EmptyCharacterPictureRenderer : ICharacterPictureRenderer
    {
        public static readonly EmptyCharacterPictureRenderer Instance = new EmptyCharacterPictureRenderer();

        public void DrawDead(ICharacterSelectionDataProvider env, int playerId, int sliceX, int sliceWidth)
        {
        }

        public void DrawSelected(ICharacterSelectionDataProvider env, int playerId, int sliceX, int sliceWidth)
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

        public void DrawDead(ICharacterSelectionDataProvider env, int playerId, int sliceX, int sliceWidth)
        {
            var pos = env.GetCharacterPanelPosition(playerId);
            var alpha = env.GetCharacterPanelAlpha(playerId);
            var img = env.SceneEnvironment.GetResource(_resNameC);
            env.SceneEnvironment.BitBlt(img, pos.X + sliceX, pos.Y, sliceWidth, img.ImageHeight, sliceX, 0, Blend.Alpha, alpha);
        }

        public void DrawSelected(ICharacterSelectionDataProvider env, int playerId, int sliceX, int sliceWidth)
        {
            var pos = env.GetCharacterPanelPosition(playerId);
            var alpha = env.GetCharacterPanelAlpha(playerId);
            if (env.IsComponentActive(null, playerId)) alpha *= env.GetFlashAlpha(playerId);
            var img = env.SceneEnvironment.GetResource(_resNameB);
            env.SceneEnvironment.BitBlt(img, pos.X + sliceX, pos.Y, sliceWidth, img.ImageHeight, sliceX, 0, Blend.Alpha, alpha);
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

        public void DrawDead(ICharacterSelectionDataProvider env, int playerId, int sliceX, int sliceWidth)
        {
            var pos = env.GetCharacterPanelPosition(playerId);
            var alpha = env.GetCharacterPanelAlpha(playerId);
            var img = env.SceneEnvironment.CreateResource(_dead);
            env.SceneEnvironment.BitBlt(img, pos.X + sliceX, pos.Y, sliceWidth, img.ImageHeight, sliceX, 0, Blend.Alpha, alpha);
        }

        public void DrawSelected(ICharacterSelectionDataProvider env, int playerId, int sliceX, int sliceWidth)
        {
            var pos = env.GetCharacterPanelPosition(playerId);
            var alpha = env.GetCharacterPanelAlpha(playerId);
            if (env.IsComponentActive(null, playerId)) alpha *= env.GetFlashAlpha(playerId);
            var img = env.SceneEnvironment.CreateResource(_sel);
            env.SceneEnvironment.BitBlt(img, pos.X + sliceX, pos.Y, sliceWidth, img.ImageHeight, sliceX, 0, Blend.Alpha, alpha);
        }
    }
}
