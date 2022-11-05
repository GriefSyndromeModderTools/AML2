using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Pat
{
    public static class AnimationIdAssignment
    {
        private static int _freeId = 10000;

        public static int AllocateIdRange(int range)
        {
            var ret = _freeId;
            _freeId += range;
            return ret;
        }

        public static void RegisterPatFileOffset(string path, int offset)
        {
            PatFileResLoaderEntry._patFiles[path] = new PatFileResLoaderEntry.PatFileInfo()
            {
                Offset = offset,
                Mod = PluginLoader.InitializingAssembly,
            };
        }
    }
}
