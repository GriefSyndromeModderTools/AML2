using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Plugins
{
    public class PluginPreset
    {
        public string Name;
        public string PluginLists;
        public List<Tuple<string, string>> Options;
    }

    public interface IPresetProvider
    {
        PluginPreset[] GetPresetList();
    }
}
