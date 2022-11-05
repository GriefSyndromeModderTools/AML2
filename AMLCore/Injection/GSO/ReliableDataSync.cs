using AMLCore.Injection.Game.Scene;
using AMLCore.Logging;
using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AMLCore.Injection.GSO
{
    internal sealed class ReliableDataSyncEntry : IEntryPointGSO, ICustomMessageHandler, ISceneEventHandler
    {
        public void Run()
        {
            CustomMessageManager.RegisterMessageHandler(ReliableDataSync.CustomeMessageId, this);
            SceneInjectionManager.RegisterSceneHandler(SystemScene.Title, this);
        }

        public void Receive(byte[] buffer, int start, int length)
        {
            var actualData = new byte[length];
            Array.Copy(buffer, start, actualData, 0, length);
            ReliableDataSync.ReceiveRaw(actualData);
        }

        public void PostInit(SceneEnvironment env)
        {
            ReliableDataSync.Sync();
        }

        public void PreUpdate()
        {
        }

        public void PostUpdate()
        {
        }

        public void Exit()
        {
        }
    }

    public static class ReliableDataSync
    {
        internal static readonly Logger Logger = new Logger("ReliableSync");
        private static readonly Dictionary<string, ReliableDataSyncChannel> _channels = new Dictionary<string, ReliableDataSyncChannel>();

        public static ReliableDataSyncChannel CreateChannel(string name)
        {
            var ret = new ReliableDataSyncChannel(name, GSOConnectionStatus.PeerCount, GSOConnectionStatus.PeerIndex);
            _channels.Add(name, ret);
            return ret;
        }

        internal const string CustomeMessageId = "ReliableDataSyncMessage";

        //Data message:
        //  byte 1
        //  byte SourcePeer
        //  int32 NameLength
        //  utf8 Name
        //  int32 Length
        //  data
        private const byte MessageData = 1;
        //Ack message:
        //  byte 2
        //  byte SourcePeer
        //  byte DestPeer
        //  int32 NameLength
        //  utf8 Name
        private const byte MessageDataAck = 2;

        internal static event Action SendTick;
        private static Thread _backgroundSender;
        private static readonly object _backgroundSenderLock = new object();

        //TODO called when showing title
        internal static void Sync()
        {
            foreach (var c in _channels.Values)
            {
                c.Sync();
            }
            if (_channels.Count > 0)
            {
                Logger.Info("Sync finished");
            }
        }

        internal static void SendData(string name, byte[] data)
        {
            var nameData = Encoding.UTF8.GetBytes(name);
            var rawData = new byte[2 + 4 + nameData.Length + 4 + data.Length];
            rawData[0] = MessageData;
            rawData[1] = (byte)GSOConnectionStatus.PeerIndex;
            Array.Copy(BitConverter.GetBytes(nameData.Length), 0, rawData, 2, 4);
            Array.Copy(nameData, 0, rawData, 6, nameData.Length);
            Array.Copy(BitConverter.GetBytes(data.Length), 0, rawData, 6 + nameData.Length, 4);
            Array.Copy(data, 0, rawData, 6 + nameData.Length + 4, data.Length);
            CustomMessageManager.SendMessage(CustomeMessageId, rawData, 0, rawData.Length);

            lock (_backgroundSenderLock)
            {
                if (_backgroundSender == null)
                {
                    _backgroundSender = ThreadHelper.StartThread(BackgroundSender);
                }
            }
        }

        private static void SendAck(string name, int peerIndex)
        {
            var nameData = Encoding.UTF8.GetBytes(name);
            var rawData = new byte[3 + 4 + nameData.Length];
            rawData[0] = MessageDataAck;
            rawData[1] = (byte)peerIndex;
            rawData[2] = (byte)GSOConnectionStatus.PeerIndex;
            Array.Copy(BitConverter.GetBytes(nameData.Length), 0, rawData, 3, 4);
            Array.Copy(nameData, 0, rawData, 7, nameData.Length);
            CustomMessageManager.SendMessage(CustomeMessageId, rawData, 0, rawData.Length);
        }

        private static string GetNameFromRawData(byte[] data, int offset, out int nextOffset)
        {
            var length = BitConverter.ToInt32(data, offset);
            nextOffset = offset + 4 + length;
            try
            {
                return Encoding.UTF8.GetString(data, offset + 4, length);
            }
            catch
            {
                return null;
            }
        }

        internal static void ReceiveRaw(byte[] rawData)
        {
            try
            {
                if (rawData[0] == MessageData)
                {
                    var channelName = GetNameFromRawData(rawData, 2, out var dataOffset);
                    if (channelName != null && _channels.TryGetValue(channelName, out var channel))
                    {
                        SendAck(channelName, rawData[1]);
                        var dataLen = BitConverter.ToInt32(rawData, dataOffset);
                        channel.Received(rawData[1], rawData, dataOffset + 4, dataLen);
                    }
                    else
                    {
                        throw new Exception("invalid data channel name");
                    }
                }
                else if (rawData[0] == MessageDataAck && rawData[1] == GSOConnectionStatus.PeerIndex)
                {
                    var channelName = GetNameFromRawData(rawData, 3, out _);
                    if (channelName != null && _channels.TryGetValue(channelName, out var channel))
                    {
                        channel.Ack(rawData[2]);
                    }
                    else
                    {
                        throw new Exception("invalid data channel name");
                    }
                }
                else
                {
                    throw new Exception("invalid message id");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Invalid message received: " + e.Message);
            }
        }

        [AMLThread(Name = "ReliableSync")]
        private static void BackgroundSender()
        {
            while (SendTick != null)
            {
                Thread.Sleep(100);
                SendTick();
            }
        }
    }

    public sealed class ReliableDataSyncChannel
    {
        private readonly string _name;
        private readonly byte[][] _data;
        private readonly bool[] _ack;
        private readonly int _index;
        private bool _synced = false;

        public byte[][] GetData() => !_synced ? throw new InvalidOperationException() : _data;

        private readonly AutoResetEvent _ackEvent = new AutoResetEvent(false);

        internal ReliableDataSyncChannel(string name, int peerCount, int peerIndex)
        {
            _name = name;
            _data = new byte[peerCount][];
            _ack = new bool[peerCount];
            _index = peerIndex;
        }

        public void SetData(byte[] data)
        {
            if (_data[_index] != null)
            {
                throw new InvalidOperationException();
            }
            _data[_index] = data.ToArray();
            _ack[_index] = true;
            ReliableDataSync.SendTick += Resend;
            Resend();
            ReliableDataSync.Logger.Info("Data sent");
        }

        private void Resend()
        {
            ReliableDataSync.SendData(_name, _data[_index]);
        }

        internal void Received(int index, byte[] data, int offset, int length)
        {
            var actualData = new byte[length];
            Array.Copy(data, offset, actualData, 0, length);
            _data[index] = actualData;
        }

        private bool AckAll()
        {
            foreach (var ack in _ack)
            {
                if (!ack)
                {
                    return false;
                }
            }
            return true;
        }

        internal void Sync()
        {
            while (true)
            {
                if (AckAll())
                {
                    break;
                }
                _ackEvent.WaitOne(100);
            }
            ReliableDataSync.SendTick -= Resend;
            _synced = true;
        }

        internal void Ack(int destPeer)
        {
            if (!_ack[destPeer])
            {
                _ack[destPeer] = true;
                _ackEvent.Set();
                ReliableDataSync.Logger.Info("Ack received from peer #" + destPeer);
                if (AckAll())
                {
                    ReliableDataSync.Logger.Info("Ack received from all peers");
                }
            }
        }
    }
}
