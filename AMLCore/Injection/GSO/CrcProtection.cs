using AMLCore.Injection.Native;
using AMLCore.Internal;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.GSO
{
    //Add crc32 redundancy check in message 0x49 (key input message).
    //Note this is compatible to the original gso.
    class CrcProtection : IMessageFilter
    {
        private byte[] _copyBuffer = new byte[35];
        private Crc32 _crc32 = new Crc32();

        public CrcProtection()
        {
            CoreLoggers.GSO.Info("Crc32 redundancy check enabled for sending");
        }

        public void FilterSend(ConnectedPeer peer, IntPtr buffer, ref int len)
        {
            var msg = Marshal.ReadByte(buffer, 1);
            if (msg == 0x49 && len == 36)
            {
                Marshal.Copy(buffer + 1, _copyBuffer, 0, 35);
                var crc = _crc32.ComputeChecksum(_copyBuffer, 0, 35);
                Marshal.WriteInt32(buffer + len, (int)crc);
                len += 4;

                //Fix the xor checksum
                uint oldChecksum = Marshal.ReadByte(buffer);
                oldChecksum ^= crc;
                oldChecksum ^= crc >> 8;
                oldChecksum ^= crc >> 16;
                oldChecksum ^= crc >> 24;
                Marshal.WriteByte(buffer, (byte)oldChecksum);
            }
        }

        public void FilterReceive(ConnectedPeer peer, IntPtr buffer, ref int len)
        {
            var msg = Marshal.ReadByte(buffer, 1);
            if (msg == 0x49 && len == 40)
            {
                Marshal.Copy(buffer + 1, _copyBuffer, 0, 35);
                var crc = _crc32.ComputeChecksum(_copyBuffer, 0, 35);
                var crcInMsg = (uint)Marshal.ReadInt32(buffer, 36);
                if (crc != crcInMsg)
                {
                    CoreLoggers.GSO.Error("Corrupted message detected. Ignored.");
                    Marshal.WriteByte(buffer, 1, 0); //Set message id to 0.
                    return;
                }
                len -= 4;

                //Fix the xor checksum
                uint oldChecksum = Marshal.ReadByte(buffer);
                oldChecksum ^= crc;
                oldChecksum ^= crc >> 8;
                oldChecksum ^= crc >> 16;
                oldChecksum ^= crc >> 24;
                Marshal.WriteByte(buffer, (byte)oldChecksum);
            }
        }
    }
}
