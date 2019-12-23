using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.SoundEffect
{
    internal class SoundEffectResLoadEntry : IEntryPointLoad
    {
        public static Dictionary<string, int> _offsetList = new Dictionary<string, int>();

        public void Run()
        {
            ResourcePack.ResourceInjection.ReplaceResourceData += ResourceInjection_ReplaceResourceData; ;
        }

        private void ResourceInjection_ReplaceResourceData(string path, ref byte[] data)
        {
            if (_offsetList.TryGetValue(path, out var offset))
            {
                data = SoundEffectListModification.Modify(data, offset);
            }
        }
    }
}
