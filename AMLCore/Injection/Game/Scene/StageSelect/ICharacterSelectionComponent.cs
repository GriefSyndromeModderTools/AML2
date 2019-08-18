using AMLCore.Injection.Engine.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.StageSelect
{
    public interface ICharacterSelectionDataProvider
    {
        SceneEnvironment SceneEnvironment { get; }
        ReadOnlyInputHandler Input { get; }
        int PlayerCount { get; }
        string GetCharacterNameSelected(int p);
        bool IsSelectedCharacterAvailable(int p);
        int GetConfigIndexSelected(int p);
        PointF GetCharacterPanelPosition(int p);
        float GetUIAlpha();
        float GetFlashAlpha(int p);
        float GetCharacterPanelAlpha(int p);
        bool IsComponentActive(ICharacterSelectionComponent component, int p);
    }

    public interface ICharacterSelectionComponent
    {
        void Init(ICharacterSelectionDataProvider p);
        void UpdateAll(ICharacterSelectionDataProvider p);
        bool IsAvailableForPlayer(ICharacterSelectionDataProvider p, int playerId);
        void UpdatePlayer(ICharacterSelectionDataProvider p, int playerId);
        void Draw(ICharacterSelectionDataProvider p);
        void ModifyPlayerType(ICharacterSelectionDataProvider p, int[] types);
        void ModifyPlayerActor(ICharacterSelectionDataProvider p, int[] types);
    }
}
