using AMLCore.Injection.Game.SoundEffect;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AMLCore.Injection.Game.Pat
{
    internal class PatFileResLoaderEntry : IEntryPointLoad
    {
        internal struct PatFileInfo
        {
            public int Offset;
            public Assembly Mod;
        }

        internal static Dictionary<string, PatFileInfo> _patFiles = new Dictionary<string, PatFileInfo>();

        public void Run()
        {
            ResourcePack.ResourceInjection.ReplaceResourceData += ResourceInjection_ReplaceResourceData;
        }

        private void ResourceInjection_ReplaceResourceData(string arg1, ref byte[] arg2)
        {
            if (!_patFiles.TryGetValue(arg1, out var info))
            {
                return;
            }
            var newData = new byte[arg2.Length];
            Buffer.BlockCopy(arg2, 0, newData, 0, newData.Length);
            if (!SoundEffectResLoadEntry._loadedOffsets.TryGetValue(info.Mod, out var dict))
            {
                dict = new Dictionary<int, int>();
            }
            PatFileParser.Modify(newData, info.Offset, dict);
            arg2 = newData;
        }
    }
}
