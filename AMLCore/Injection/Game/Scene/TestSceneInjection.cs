using AMLCore.Injection.Engine.Input;
using AMLCore.Injection.Game.Scene.StageSelect;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AMLCore.Injection.Game.Scene
{
    internal class TestSceneInjection : IEntryPointLoad
    {
        public void Run()
        {
            MessageBox.Show("Start");
            NewStageSelect.UseNewStageSelect();
            NewStageSelect._componentList.Add(new LevelSelectionComponent());
        }
    }
}
