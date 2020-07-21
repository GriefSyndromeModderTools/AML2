using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.SoundEffect
{
    public static class SoundEffectIdAssignment
    {
        private static int _freeId = 7000;

        //This is a dirty compatibility fix for PatchouliMod 1.0, which incorrectly allocates 1000 but uses 3000.
        //Later mods should avoid allocating 1000 (either 1001 or 999 is fine).
        public static int AllocateIdRange(int count)
        {
            if (count == 1000)
            {
                count = 3000;
            }
            var ret = _freeId;
            _freeId += count;
            return ret;
        }

        //The correct implementation without the fix.
        public static int AllocateIdRange2(int count)
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
