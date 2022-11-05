using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Replay
{
    public static class GSOReplay
    {
        public static bool IsReplaying { get; internal set; } = false;
        public static bool IsAMLFormat => _readSections != null && _readSections.Count > 0;

        internal static Dictionary<string, byte[]> _readSections;
        private static Dictionary<string, int> _writeSectionIds = new Dictionary<string, int>();

        public static byte[] ReadSection(string section)
        {
            if (_readSections != null && _readSections.TryGetValue(section, out var ret))
            {
                return ret;
            }
            return null;
        }

        private static bool TryGetStream(out ReplayFileStream stream)
        {
            stream = ReplayRecorderEntry._recorder?._stream;
            return stream != null;
        }

        public static void WriteSection(string section, byte[] data, int offset, int length)
        {
            if (!TryGetStream(out var stream))
            {
                return;
            }
            if (!_writeSectionIds.TryGetValue(section, out var id))
            {
                id = stream.CreateSection(section);
                _writeSectionIds[section] = id;
            }
            stream.AppendSection(id, data, offset, length);
        }
    }
}
