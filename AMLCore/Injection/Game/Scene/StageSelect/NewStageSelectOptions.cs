using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.StageSelect
{
    public static class NewStageSelectOptions
    {
        public enum ChooseDeadAsQBOption
        {
            Never,
            NotEnoughCharacter,
            Always,
        }

        public static void EnableNewStageSelect()
        {
            NewStageSelect.UseNewStageSelect();
        }

        public static void CanChooseDeadAsQB(ChooseDeadAsQBOption option)
        {
            switch (option)
            {
                case ChooseDeadAsQBOption.Always:
                    NewStageSelect._alwaysChooseDeadAsQB = true;
                    NewStageSelect._noAvailableChooseDeadAsQB = true;
                    break;
                case ChooseDeadAsQBOption.NotEnoughCharacter:
                    NewStageSelect._alwaysChooseDeadAsQB = false;
                    NewStageSelect._noAvailableChooseDeadAsQB = true;
                    break;
                default:
                    NewStageSelect._alwaysChooseDeadAsQB = false;
                    NewStageSelect._noAvailableChooseDeadAsQB = false;
                    break;
            }
        }

        public static void AllowSameCharacter(bool val)
        {
            NewStageSelect._allowSameChar = val;
        }

        public static void AllowQBOnlyGame(bool val)
        {
            NewStageSelect._allowQBOnlyGame = val;
        }

        public static void AddComponent(ICharacterSelectionComponent comp)
        {
            NewStageSelect._componentList.Add(comp);
        }
    }
}
