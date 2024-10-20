using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Internal
{
    public struct PresetWithSource
    {
        public string Preset;
        public string Source;
    }

    internal sealed class PresetSelection
    {
        public Preset DefaultPreset { get; }
        public List<PresetWithSource> SelectedPresets { get; }

        public PresetSelection(Preset defaultPreset, List<PresetWithSource> selectedPresets)
        {
            DefaultPreset = defaultPreset;
            SelectedPresets = selectedPresets;
        }

        public PresetSelection(Preset defaultPreset, IEnumerable<Preset> presets)
        {
            DefaultPreset = defaultPreset;
            SelectedPresets = new List<PresetWithSource>();
            foreach (var p in presets)
            {
                SelectedPresets.Add(new PresetWithSource
                {
                    Preset = p.Name,
                    Source = p.SourcePlugin,
                });
            }
        }
    }
}
