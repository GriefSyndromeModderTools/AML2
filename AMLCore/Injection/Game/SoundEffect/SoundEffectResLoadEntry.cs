using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AMLCore.Injection.Game.SoundEffect
{
    internal class SoundEffectResLoadEntry : IEntryPointLoad
    {
        internal struct SEInfo
        {
            public int Offset;
            public Assembly Mod;
        }

        internal static Dictionary<string, SEInfo> _seFiles = new Dictionary<string, SEInfo>();
        public static Dictionary<Assembly, Dictionary<int, int>> _loadedOffsets = new Dictionary<Assembly, Dictionary<int, int>>();

        public void Run()
        {
            ResourcePack.ResourceInjection.ReplaceResourceData += ResourceInjection_ReplaceResourceData; ;
        }

        private void ResourceInjection_ReplaceResourceData(string path, ref byte[] data)
        {
            if (_seFiles.TryGetValue(path, out var info))
            {
                if (!_loadedOffsets.TryGetValue(info.Mod, out var dict))
                {
                    dict = new Dictionary<int, int>();
                    _loadedOffsets.Add(info.Mod, dict);
                }
                data = SoundEffectListModification.Modify(data, info.Offset, dict);
            }
        }
    }
}
