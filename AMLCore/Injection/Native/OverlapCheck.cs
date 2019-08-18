using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Native
{
    internal class OverlapCheck
    {
        private class Segment
        {
            public readonly List<Tuple<int, int>> Modifications = new List<Tuple<int, int>>();
        }

        private static readonly object _lock = new object();
        private static readonly Dictionary<int, Segment> _segments = new Dictionary<int, Segment>();

        public static void Add(IntPtr ptr, int len)
        {
            if (len > 0xFFFF)
            {
                throw new Exception("Modification length exceeds limits.");
            }
            int p = ptr.ToInt32();
            int s = p >> 16;
            int o = p & 0xFFFF;
            if (o + len > 0x10000)
            {
                int len1 = 0x10000 - o;
                Add(s, o, len1);
                Add(s + 1, 0, len - len1);
            }
        }

        private static void Add(int segment, int offset, int len)
        {
            lock (_lock)
            {
                if (!_segments.TryGetValue(segment, out var s))
                {
                    s = new Segment();
                    _segments.Add(segment, s);
                }
                foreach (var m in s.Modifications)
                {
                    if (m.Item1 < offset + len && offset < m.Item1 + m.Item2)
                    {
                        throw new Exception("Modification overlap check fails.");
                    }
                }
                s.Modifications.Add(new Tuple<int, int>(offset, len));
            }
        }
    }
}
