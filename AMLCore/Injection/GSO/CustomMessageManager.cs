using AMLCore.Internal;
using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.GSO
{
    public static class CustomMessageManager
    {
        internal class MessageFilter : IMessageFilter
        {
            public void FilterReceive(ConnectedPeer peer, IntPtr buffer, ref int len)
            {
                var msg = Marshal.ReadByte(buffer, 1);
                if (msg != InternalMessageId.CustomMessageClient &&
                    msg != InternalMessageId.CustomMessageServer)
                {
                    return;
                }
                if (len > _buffer.Length) return;
                lock (_buffer)
                {
                    Marshal.Copy(buffer, _buffer, 0, len);
                    FilterMessage(len);
                }
            }

            public void FilterSend(ConnectedPeer peer, IntPtr buffer, ref int len)
            {
            }
        }

        private struct MessageInfo
        {
            public string Name;
            public ICustomMessageHandler Handler;
        }

        private static readonly Dictionary<uint, MessageInfo> _messageInfoList = new Dictionary<uint, MessageInfo>();
        private static readonly Dictionary<uint, string> _messageIdHash = new Dictionary<uint, string>();
        private static readonly byte[] _buffer = new byte[1000];
        private static readonly Crc32 _crc32 = new Crc32();

        private static uint MessageNameHash(string str)
        {
            uint h = 5381;
            for (int i = 0; i < str.Length; ++i)
            {
                h = (h << 5) + h + str[i];
            }
            return h;
        }

        private static int NewMessageIdInternal(string name, ICustomMessageHandler handler)
        {
            lock (_messageInfoList)
            {
                var hash = MessageNameHash(name);
                if (_messageIdHash.ContainsKey(hash))
                {
                    throw new Exception("custom message id hash collision");
                }
                _messageInfoList.Add(hash, new MessageInfo
                {
                    Name = name,
                    Handler = handler,
                });
                CoreLoggers.GSO.Info($"registered custom message handler for {name}({hash:X8}) with {handler.GetType().FullName}");
                return _messageInfoList.Count - 1;
            }
        }

        public static void RegisterMessageHandler(string name, ICustomMessageHandler handler)
        {
            NewMessageIdInternal(name, handler);
        }

        private static int WriteMessageToBuffer(int id, uint customIdHash, byte[] data, int start, int length)
        {
            _buffer[0] = 0;
            _buffer[1] = (byte)id;
            _buffer[2] = (byte)((customIdHash >> 0) & 0xFF);
            _buffer[3] = (byte)((customIdHash >> 8) & 0xFF);
            _buffer[4] = (byte)((customIdHash >> 16) & 0xFF);
            _buffer[5] = (byte)((customIdHash >> 24) & 0xFF);
            _buffer[6] = (byte)length;
            Array.Copy(data, start, _buffer, 7, length);
            var checksum = _crc32.ComputeChecksum(_buffer, 1, length + 6);
            Array.Copy(BitConverter.GetBytes(checksum), 0, _buffer, length + 7, 4);
            return length + 11;
        }

        private static bool ReadMessageFromBuffer(int totalLen, out int id, out uint customIdHash, out int start, out int length)
        {
            id = _buffer[1];
            customIdHash = BitConverter.ToUInt32(_buffer, 2);
            length = _buffer[6];
            start = 7;
            if (_buffer[0] != 0) return false;
            if (length + 11 != totalLen)
            {
                return false;
            }
            var checksumInMsg = BitConverter.ToUInt32(_buffer, length + 7);
            var checksum = _crc32.ComputeChecksum(_buffer, 1, length + 6);
            if (checksum != checksumInMsg)
            {
                return false;
            }
            return true;
        }

        private static void RewriteServerMessage(int id)
        {
            _buffer[1] = (byte)id;
            var length = _buffer[6];
            var checksum = _crc32.ComputeChecksum(_buffer, 1, length + 6);
            Array.Copy(BitConverter.GetBytes(checksum), 0, _buffer, length + 7, 4);
        }

        //Note that custom message is not reliable.
        public static void SendMessage(string id, byte[] data, int start, int length)
        {
            var hash = MessageNameHash(id);
            if (!_messageInfoList.ContainsKey(hash))
            {
                throw new Exception("invalid message id");
            }
            if (length >= 256)
            {
                throw new Exception("custom message too long");
            }
            lock (_buffer)
            {
                if (GSOConnectionStatus.IsServer)
                {
                    var totalLen = WriteMessageToBuffer(InternalMessageId.CustomMessageServer, hash, data, start, length);
                    GSOConnectionStatus.ServerStatus.Send(_buffer, 0, totalLen);
                }
                else
                {
                    var totalLen = WriteMessageToBuffer(InternalMessageId.CustomMessageClient, hash, data, start, length);
                    GSOConnectionStatus.ClientStatus.Send(_buffer, 0, totalLen);
                }
            }
        }

        internal static void FilterMessage(int length)
        {
            if (!ReadMessageFromBuffer(length, out var id, out var customId, out var start, out int msgLength))
            {
                CoreLoggers.GSO.Error("invalid custom message");
                return;
            }
            if (id == InternalMessageId.CustomMessageClient)
            {
                if (GSOConnectionStatus.IsClient)
                {
                    CoreLoggers.GSO.Error("invalid custom message");
                    return;
                }

                //Broadcast to clients.
                RewriteServerMessage(InternalMessageId.CustomMessageServer);
                GSOConnectionStatus.ServerStatus.Send(_buffer, 0, length);
            }
            else
            {
                if (GSOConnectionStatus.IsServer)
                {
                    CoreLoggers.GSO.Error("invalid custom message");
                    return;
                }
            }
            //Handle mesage.
            if (!_messageInfoList.TryGetValue(customId, out var msgInfo))
            {
                CoreLoggers.GSO.Error($"unknown custom message received with id {customId}");
                return;
            }
            msgInfo.Handler.Receive(_buffer, start, msgLength);
        }
    }
}
