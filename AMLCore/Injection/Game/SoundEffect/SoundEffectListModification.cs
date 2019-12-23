using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.SoundEffect
{
    internal class SoundEffectListModification
    {
        public static byte[] Modify(byte[] data, int offset)
        {
            Encrypt(data);
            var str = Encoding.ASCII.GetString(data);
            var lines = str.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                if (line[0] == '#')
                {
                    sb.AppendLine(line);
                    continue;
                }

                var values = line.Split(',');
                if (!int.TryParse(values[0], out var id))
                {
                    sb.AppendLine(line);
                    continue;
                }

                values[0] = (id + offset).ToString();
                sb.AppendLine(string.Join(",", values));
            }
            var newData = Encoding.ASCII.GetBytes(sb.ToString());
            Encrypt(newData);
            return newData;
        }

        private static void Encrypt(byte[] data)
        {
            byte cl = 0x8B, dl = 0x71;
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] ^= cl;
                cl += dl;
                dl += 0x95;
            }
        }
    }
}
