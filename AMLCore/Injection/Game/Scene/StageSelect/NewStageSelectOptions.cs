﻿using System;
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

        public static bool IsSameCharacterAllowed
        {
            get => NewStageSelect._allowSameChar;
            set => NewStageSelect._allowSameChar = value;
        }

        public static void AllowQBOnlyGame(bool val)
        {
            NewStageSelect._allowQBOnlyGame = val;
        }

        public static void AddComponent(ICharacterSelectionComponent comp)
        {
            NewStageSelect._componentList.Add(comp);
        }

        public static void UnlockAllStages()
        {
            NewStageSelect._unlockAllStages = true;
        }

        public static void AddQB()
        {
            NewStageSelect._includeQB = true;
        }

        public static void AddCharacterRenderer(int type, ICharacterPictureRenderer renderer)
        {
            NewStageSelect._characterRenderer.Add(type, renderer);
        }
    }
}
