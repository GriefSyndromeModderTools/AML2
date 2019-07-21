using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.CharacterInfo
{
    class CharacterLevelInjectEntry : IEntryPointPostload
    {
        public void Run()
        {
            //inject ADD_Exp and ADD_Level functions
            //see LevelSelectionComponent class
        }
    }
}
