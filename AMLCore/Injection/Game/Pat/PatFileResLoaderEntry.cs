using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Pat
{
    internal class PatFileResLoaderEntry : IEntryPointLoad
    {
        internal static Dictionary<string, int> _offset = new Dictionary<string, int>();

        public void Run()
        {
            ResourcePack.ResourceInjection.ReplaceResourceData += ResourceInjection_ReplaceResourceData;
        }

        private void ResourceInjection_ReplaceResourceData(string arg1, ref byte[] arg2)
        {
            if (!_offset.TryGetValue(arg1, out var offset))
            {
                return;
            }
            var newData = new byte[arg2.Length];
            Buffer.BlockCopy(arg2, 0, newData, 0, newData.Length);
            PatFileParser.Modify(newData, offset);
            arg2 = newData;
        }
    }
}
