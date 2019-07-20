using AMLCore.Injection.Engine.Input;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene
{
    internal class TestSceneInjection : IEntryPointLoad
    {
        public void Run()
        {
            System.Windows.Forms.MessageBox.Show("Start");
            StageSelect.NewStageSelect.UseNewStageSelect();
        }
    }
}
