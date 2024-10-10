using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace AMLCore.Internal
{
    public class CommonArguments
    {
        public string Mods { get; set; }
        public List<Tuple<string, string>> Options { get; protected set; }
        public Dictionary<string, string> ModVersions { get; protected set; }
        internal PresetSelection PresetSelection { get; private set; }

        public CommonArguments()
        {
            Mods = "";
            Options = new List<Tuple<string, string>>();
            ModVersions = new Dictionary<string, string>();
            PresetSelection = null;
        }
        
        public CommonArguments(IEnumerable<CommonArguments> aa)
        {
            MergeFrom(aa);
        }

        private void MergeFrom(IEnumerable<CommonArguments> aa)
        {
            Mods = string.Join(",", aa.SelectMany(a => a.SplitMods()).Distinct());
            Options = aa.SelectMany(a => a.Options).Distinct().ToList();
            ModVersions = new Dictionary<string, string>(); //Merging presets, no version info.
        }

        //This method is provided for binary compatibility with Launcher.exe.
        //Upon updating, the exe file has a much higher chance to be identified as a malware.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public byte[] Serialize(bool includeModVersions)
        {
            return Serialize(includeModVersions, false);
        }

        public byte[] Serialize(bool includeModVersions = false, bool includePresetSelection = false)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(0);

                CommonArguments baseArgument =
                    includePresetSelection && PresetSelection != null ? PresetSelection.DefaultPreset : this;

                //Mods and options.
                bw.Write(baseArgument.Mods != null);
                if (baseArgument.Mods != null)
                {
                    bw.Write(baseArgument.Mods);
                }
                bw.Write(baseArgument.Options.Count);
                foreach (var o in baseArgument.Options)
                {
                    if (o.Item1 != null && o.Item2 != null)
                    {
                        bw.Write(o.Item1);
                        bw.Write(o.Item2);
                    }
                }

                //Mod version list.
                if (includeModVersions && baseArgument.ModVersions.Count > 0)
                {
                    bw.Write(baseArgument.ModVersions.Count);
                    foreach (var o in baseArgument.ModVersions)
                    {
                        if (o.Key != null && o.Value != null)
                        {
                            bw.Write(o.Key);
                            bw.Write(o.Value);
                        }
                    }
                }
                else
                {
                    bw.Write(0);
                }

                //Preset selection.
                if (includePresetSelection && PresetSelection != null)
                {
                    bw.Write(PresetSelection.SelectedPresets.Count);
                    foreach (var p in PresetSelection.SelectedPresets)
                    {
                        if (!string.IsNullOrEmpty(p.Preset))
                        {
                            bw.Write(p.Preset);
                            bw.Write(p.Source ?? string.Empty);
                        }
                    }
                }
                else
                {
                    bw.Write(0);
                }

                ms.Position = 0;
                ms.Write(BitConverter.GetBytes(ms.Length - 4), 0, 4);
                return ms.ToArray();
            }
        }

        public void Read(BinaryReader br, bool forcePresetSelection = false)
        {
            //Mods and options.
            Mods = br.ReadBoolean() ? br.ReadString() : null;
            Options = new List<Tuple<string, string>>();
            int c = br.ReadInt32();
            for (int i = 0; i < c; ++i)
            {
                var key = br.ReadString();
                var val = br.ReadString();
                Options.Add(new Tuple<string, string>(key, val));
            }

            //Mod version list.
            c = br.ReadInt32();
            for (int i = 0; i < c; ++i)
            {
                var key = br.ReadString();
                var val = br.ReadString();
                ModVersions.Add(key, val);
            }

            //Preset selection.
            c = br.ReadInt32();
            if (c > 0 || forcePresetSelection)
            {
                var selectedPresets = new List<PresetWithSource>();
                for (int i = 0; i < c; ++i)
                {
                    selectedPresets.Add(new PresetWithSource
                    {
                        Preset = br.ReadString(),
                        Source = br.ReadString(),
                    });
                }

                //Move Mods and Options.
                //Don't move ModVersions.
                var defaultPreset = Preset.CreateDefaultPreset();
                defaultPreset.Mods = Mods;
                defaultPreset.Options = Options;
                Mods = null;
                Options = new List<Tuple<string, string>>();
                PresetSelection = new PresetSelection(defaultPreset, selectedPresets);

                ResolvePresetSelection();
            }
        }

        public string[] SplitMods()
        {
            return Mods?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
        }

        private string[] GetModFileListFromMods()
        {
            var d = PathHelper.GetPath("aml/mods");
            List<string> dlls = new List<string>();
            foreach (var m in SplitMods())
            {
                var f = Path.Combine(d, m + ".dll");
                if (File.Exists(f))
                {
                    dlls.Add(f);
                }
                else
                {
                    CoreLoggers.Loader.Error("cannot initialize mod {0}", m);
                }
            }
            return dlls.ToArray();
        }

        public string[] GetPluginFiles()
        {
            return GetModFileListFromMods();
        }

        private void ResolvePresetSelection()
        {
            if (PresetSelection == null)
            {
                return;
            }
            if (PresetSelection.SelectedPresets.Count == 0)
            {
                Mods = PresetSelection.DefaultPreset.Mods.ToString();
                Options = PresetSelection.DefaultPreset.Options.ToList();
                return;
            }

            var loadedContainerPresets = new Dictionary<string, Dictionary<string, Preset>>();
            var presetsWithoutSource = new Dictionary<string, Preset>();
            var tempPresetList = new List<Preset>();
            foreach (var p in PresetSelection.SelectedPresets)
            {
                if (string.IsNullOrEmpty(p.Source))
                {
                    presetsWithoutSource.Add(p.Preset, null);
                }
                if (loadedContainerPresets.ContainsKey(p.Source))
                {
                    continue;
                }
                var container = PluginLoader.GetTemporaryContainer(p.Source);
                if (container != null)
                {
                    CoreLoggers.Loader.Error("Cannot find preset " + p.Preset + " from " + p.Source);
                    continue;
                }
                tempPresetList.Clear();
                container.CollectPresets(tempPresetList);
                loadedContainerPresets.Add(p.Source, tempPresetList.ToDictionary(pp => pp.Name));
            }

            if (presetsWithoutSource.Count > 0)
            {
                int remaining = presetsWithoutSource.Count;
                foreach (var pi in Preset.GetPresetsInfoFromJson())
                {
                    if (presetsWithoutSource.ContainsKey(pi.Name))
                    {
                        presetsWithoutSource[pi.Name] = new Preset(pi, false);
                    }
                    if (--remaining == 0)
                    {
                        break;
                    }
                }
            }

            //Note that we must keep the order of selection.
            var additionalPresets = new List<Preset>();
            additionalPresets.Add(PresetSelection.DefaultPreset);
            foreach (var p in PresetSelection.SelectedPresets)
            {
                if (presetsWithoutSource.TryGetValue(p.Preset, out var preset))
                {
                    additionalPresets.Add(preset);
                }
                else if (loadedContainerPresets.TryGetValue(p.Source, out var presets) &&
                    presets.TryGetValue(p.Preset, out preset))
                {
                    additionalPresets.Add(preset);
                }
            }

            //Merge presets into this.
            MergeFrom(additionalPresets);
        }

        internal void SetPluginOptions(PluginContainer[] plugins)
        {
            foreach (var p in plugins)
            {
                p.ResetOption();
            }

            Dictionary<string, PluginContainer> pluginDict = plugins
                .ToDictionary(p => p.AssemblyName);
            foreach (var o in Options)
            {
                var key = o.Item1;
                var seg = key.Split('.');
                if (seg.Length != 2)
                {
                    continue;
                }
                if (!pluginDict.TryGetValue(seg[0], out var p) || !p.HasOptions)
                {
                    CoreLoggers.Loader.Error("unrecognized option {0}", key);
                    continue;
                }
                p.AddOption(seg[1], o.Item2);
            }
        }

        internal void GetPluginOptions(PluginContainer[] plugins)
        {
            Mods = String.Join(",", plugins.Select(p => p.AssemblyName));
            var options = new List<Tuple<string, string>>();
            var currentPlugin = "";
            Action<string, string> append = (string a, string b) =>
            {
                options.Add(new Tuple<string, string>(currentPlugin + "." + a, b));
            };
            foreach (var p in plugins)
            {
                if (p.HasOptions)
                {
                    currentPlugin = p.AssemblyName;
                    p.GetOptions(append);
                }
            }
            Options = options;
        }

        protected void ParseArgumentList(string[] args)
        {
            foreach (var a in args)
            {
                ParseArgument(a);
            }
        }

        private void ParseArgument(string arg)
        {
            //if (arg.Contains('"') || arg.Contains('\\'))
            //{
            //    CoreLoggers.Loader.Error("invalid launcher argument {0}", arg);
            //    return;
            //}
            if (!arg.Contains('='))
            {
                ParseKeyValuePair(arg, null);
            }
            else
            {
                int index = arg.IndexOf('=');
                ParseKeyValuePair(arg.Substring(0, index).Trim(),
                    arg.Substring(index + 1).Trim());
            }
        }

        protected virtual void ParseKeyValuePair(string key, string value)
        {
            if (key == "Mods")
            {
                if (String.IsNullOrEmpty(Mods))
                {
                    Mods = value;
                }
                else
                {
                    Mods = Mods + "," + value;
                }
            }
            else
            {
                Options.Add(new Tuple<string, string>(key, value));
            }
        }
    }
}
