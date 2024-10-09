using AMLCore.Internal;
using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AMLCore.Plugins
{
    internal class Preset : CommonArguments
    {
        public class LoadingPreset
        {
            public string Name;
            public string Mods;
            public string[] Options;
        }

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

        public Preset(LoadingPreset pi, bool editable)
        {
            Name = pi.Name;
            Editable = editable;
            Mods = pi.Mods;
            Options.AddRange(pi.Options
                .Where(oo => oo.Length > 0 && oo.Contains("="))
                .Select(oo => oo.Split('='))
                .Select(oo => new Tuple<string, string>(oo[0], oo[1])));
        }

        public static Preset CreateDefaultPreset()
        {
            return new Preset("(默认)", true);
        }

        public void ParseModsAndOptions(string[] args)
        {
            Mods = "";
            Options.Clear();
            ParseArgumentList(args);
        }

        public static IEnumerable<LoadingPreset> GetPresetsInfoFromJson()
        {
            foreach (var file in Directory.EnumerateFiles(PathHelper.GetPath("aml/presets/"), "*.*"))
            {
                LoadingPreset[] list;
                try
                {
                    var str = File.ReadAllText(file);
                    list = JsonSerialization.Deserialize<LoadingPreset[]>(str);
                }
                catch
                {
                    CoreLoggers.Loader.Error("Cannot load preset file {0}", file);
                    continue;
                }
                foreach (var p in list)
                {
                    yield return p;
                }
            }
        }

        public static void GetPresetsFromJson(List<Preset> presets, bool editable)
        {
            foreach (var pi in GetPresetsInfoFromJson())
            {
                presets.Add(new Preset(pi, editable));
            }
        }
    }
}
