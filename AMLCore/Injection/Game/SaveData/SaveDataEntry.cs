using AMLCore.Injection.Engine.File;
using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.SaveData
{
    internal class SaveDataEntry : IEntryPointPreload
    {
        public void Run()
        {
            FileReplacement.RegisterFile(PathHelper.GetPath("save/save0.dat"),
                new SaveDataHelper.SaveFile());
        }
    }
}
