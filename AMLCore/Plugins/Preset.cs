using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Plugins
{
    internal class Preset : CommonArguments
    {
        public string Name { get; set; }
        public bool Editable { get; private set; }
        public string SourcePlugin { get; set; }

        public Preset(string name, bool editable)
        {
            Name = name;
            Editable = editable;
        }

        public Preset(IEnumerable<Preset> list) : base(list)
        {
            Name = null;
        }

        public void ParseModsAndOptions(string[] args)
        {
            Mods = "";
            Options.Clear();
            ParseArgumentList(args);
        }
    }
}
