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
    class CrcProtection : IEntryPointLoad
    {
        public void Run()
        {
            PostGSOInjection.Run(() =>
            {
                if (!PostGSOInjection.IsGSO) return;

                new InjectSend();
                new InjectReceive();

                CoreLoggers.GSO.Info("Crc32 redundancy check enabled for sending");
            });
        }

        private delegate int SendToDelegate(IntPtr socket, IntPtr buffer, int len, int flags, IntPtr addr, int addrLen);
        private delegate int ReceiveFromDelegate(IntPtr socket, IntPtr buffer, int len, int flags, IntPtr addr, IntPtr addrLen);

        private class InjectSend : FunctionPointerInjection<SendToDelegate>
        {
            public InjectSend() : base(AddressHelper.Code("gso", 0x1C274))
            {
            }
            private byte[] _copyBuffer = new byte[1024];
            private Crc32 _crc32 = new Crc32();

            protected override void Triggered(NativeEnvironment env)
            {
                var socket = env.GetParameterP(0);
                var buffer = env.GetParameterP(1);
                var len = env.GetParameterI(2);
                var flags = env.GetParameterI(3);
                var addr = env.GetParameterP(4);
                var addrLen = env.GetParameterI(5);
                if (len > 2)
                {
                    var msg = Marshal.ReadByte(buffer, 1);
                    if (msg == 0x49)
                    {
                        //Exclude the gso checksum, which we need to modify.
                        Marshal.Copy(buffer + 1, _copyBuffer, 0, len - 1);
                        var crc = _crc32.ComputeChecksum(_copyBuffer, 0, len - 1);
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
                var ret = Original(socket, buffer, len, flags, addr, addrLen);
                env.SetReturnValue(ret);
            }
        }

        private class InjectReceive : FunctionPointerInjection<ReceiveFromDelegate>
        {
            public InjectReceive() : base(AddressHelper.Code("gso", 0x1C288))
            {
            }
            private byte[] _copyBuffer = new byte[35];
            private Crc32 _crc32 = new Crc32();

            protected override void Triggered(NativeEnvironment env)
            {
                var socket = env.GetParameterP(0);
                var buffer = env.GetParameterP(1);
                var len = env.GetParameterI(2);
                var flags = env.GetParameterI(3);
                var addr = env.GetParameterP(4);
                var addrLen = env.GetParameterP(5);
                var ret = Original(socket, buffer, len, flags, addr, addrLen);
                if (ret > 2)
                {
                    if (Marshal.ReadByte(buffer, 1) == 0x49)
                     {
                        if (ret == 40)
                        {
                            Marshal.Copy(buffer + 1, _copyBuffer, 0, 35);
                            var crc = _crc32.ComputeChecksum(_copyBuffer, 0, 35);
                            var crcInMsg = (uint)Marshal.ReadInt32(buffer, 36);
                            if (crc != crcInMsg)
                            {
                                CoreLoggers.GSO.Error("Corrupted message detected. Ignored.");
                                Marshal.WriteByte(buffer, 1, 0); //Set message id to 0.
                            }
                            ret -= 4;

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
                env.SetReturnValue(ret);
            }
        }
    }
}
