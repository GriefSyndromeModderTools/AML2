using AMLCore.Injection.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Misc
{
    public class Crc32
    {
        private uint[] table;
        private byte[] buffer = new byte[1000];

        public uint ComputeChecksum(byte[] bytes, int start, int len)
        {
            uint crc = 0xffffffff;
            for (int i = start; i < start + len; ++i)
            {
                byte index = (byte)(((crc) & 0xff) ^ bytes[i]);
                crc = (uint)((crc >> 8) ^ table[index]);
            }
            return ~crc;
        }

        public uint ComputeChecksum(Stream stream, int length = -1)
        {
            uint crc = 0xffffffff;
            int remaining = length;
            while (remaining != 0)
            {
                var toRead = remaining == -1 ? buffer.Length : Math.Min(buffer.Length, remaining);
                var read = stream.Read(buffer, 0, toRead);

                for (int i = 0; i < read; ++i)
                {
                    byte index = (byte)(((crc) & 0xff) ^ buffer[i]);
                    crc = (uint)((crc >> 8) ^ table[index]);
                }
                remaining = remaining == -1 ? -1 : remaining - read;
                if (read == 0)
                {
                    break;
                }
            }
            return ~crc;
        }

        public Crc32()
        {
            uint poly = 0xedb88320;
            table = new uint[256];
            uint temp;
            for (uint i = 0; i < table.Length; ++i)
            {
                temp = i;
                for (int j = 8; j > 0; --j)
                {
                    if ((temp & 1) == 1)
                    {
                        temp = (uint)((temp >> 1) ^ poly);
                    }
                    else
                    {
                        temp >>= 1;
                    }
                }
                table[i] = temp;
            }
        }
    }
}
