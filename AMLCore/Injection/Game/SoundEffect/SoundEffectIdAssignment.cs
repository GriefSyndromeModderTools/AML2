using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.SoundEffect
{
    public static class SoundEffectIdAssignment
    {
        private static int _freeId = 7000;

        public static int AllocateIdRange(int count)
        {
            var ret = _freeId;
            _freeId += count;
            return ret;
        }

        public static void RegisterSEFileOffset(string path, int offset)
        {
            SoundEffectResLoadEntry._offsetList[path] = offset;
        }
    }
}
