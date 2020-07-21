using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Replay
{
    internal class ReplayFile
    {
        private static UInt32 Magic = 0x50525347;

        public int BaseLap;
        public UInt16[] InputData;
        public ChatMessage[] ChatMessages;
        public Dictionary<string, byte[]> AMLSections;

        public class ChatMessage
        {
            public UInt32 Time;
            public UInt32 Player;
            public string Message;
        }

        public static void Test()
        {
            var rep = new ReplayFile(1000, 1000);
            rep.ChatMessages = new[]
            {
                new ChatMessage
                {
                    Time = 10,
                    Player = 0,
                    Message = new string(new char[] { '1', '2', '3' }.Concat(Enumerable.Repeat('\0', 200)).ToArray(), 0, 203),
                },
                new ChatMessage
                {
                    Time = 80,
                    Player = 1,
                    Message = new string(new char[] { '3', '2', '1' }.Concat(Enumerable.Repeat('\0', 200)).ToArray(), 0, 203),
                },
            };
            rep.Save(@"E:\1.rep");
        }

        public ReplayFile(string filename) : this(File.ReadAllBytes(filename))
        {
        }

        public ReplayFile(byte[] data)
        {
            if (BitConverter.ToUInt32(data, 0) != Magic)
            {
                throw new Exception("not a replay file");
            }
            var baseLap = BitConverter.ToUInt32(data, 4);
            var length = BitConverter.ToUInt32(data, 8);
            var chat = BitConverter.ToUInt32(data, 12);

            if ((length % 3) != 0)
            {
                throw new Exception("invalid file length");
            }

            BaseLap = (int)baseLap;

            InputData = new UInt16[length];
            Buffer.BlockCopy(data, 16, InputData, 0, (int)length * 2);

            int chatOffset = 16 + (int)length * 2;
            var chatList = new List<ChatMessage>();

            for (int i = 0; i < chat; ++i)
            {
                var c = ReadChat(data, ref chatOffset);
                if (c.Time != int.MaxValue)
                {
                    chatList.Add(c);
                }
            }
            ChatMessages = chatList.ToArray();
            if (chatOffset < data.Length)
            {
                ReadChat(data, ref chatOffset);
            }

            ReadAMLData(data, chatOffset);
        }

        public ReplayFile(int lap, int time)
        {
            BaseLap = lap;
            InputData = new UInt16[time * 3];
            ChatMessages = new ChatMessage[0];
        }

        private static ChatMessage ReadChat(byte[] data, ref int offset)
        {
            var t = BitConverter.ToUInt32(data, offset);
            var p = BitConverter.ToUInt32(data, offset + 4);
            var l = BitConverter.ToUInt32(data, offset + 8);
            char[] str = new char[l];
            Buffer.BlockCopy(data, offset + 12, str, 0, (int)l * 2);
            offset += (int)(12 + ((l + 1) * 2));
            return new ChatMessage
            {
                Time = t,
                Player = p,
                Message = new string(str),
            };
        }

        private void ReadAMLData(byte[] data, int start)
        {
            Dictionary<uint, byte[]> blocks = new Dictionary<uint, byte[]>();
            while (start < data.Length)
            {
                uint header = BitConverter.ToUInt32(data, start);
                var d = new byte[252];
                Array.Copy(data, start + 4, d, 0, 252);
                var section = header >> 24;
                //Section 255 is reserved in current version.
                //Header 0x00FFFFFF is empty block.
                if (section != 255 && header != 0xFFFFFF)
                {
                    blocks.Add(header, d);
                }
                start += 256;
            }
            var sections = blocks
                .GroupBy(bb => (int)(bb.Key >> 24), (kk, ee) => new {
                    Section = kk,
                    Data = ee
                        .OrderBy(bbb => bbb.Key)
                        .Select(bbb => bbb.Value)
                        .SelectMany(bbb => bbb)
                })
                .ToDictionary(ss => ss.Section, ss => ss.Data.Take(ss.Data.Count() - 252 + ss.Data.Last()).ToArray());
            List<string> sectionId = new List<string>();
            var sectionIdData = sections[0];
            int s0pos = 0;
            while (s0pos < sectionIdData.Length)
            {
                var s0end = Array.FindIndex(sectionIdData, s0pos, ii => ii == 0);
                var str = Encoding.UTF8.GetString(sectionIdData, s0pos, s0end - s0pos);
                s0pos = s0end + 1;
                sectionId.Add(str);
            }
            AMLSections = sections
                .Where(ss => ss.Key != 0)
                .ToDictionary(ss => sectionId[ss.Key - 1], ss => ss.Value);
        }

        public void Save(string filename)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            using (var file = File.OpenWrite(filename))
            {
                Save(file);
            }
        }

        public void Save(Stream stream)
        {
            using (var w = new BinaryWriter(stream))
            {
                w.Write(Magic);
                w.Write(BaseLap);
                w.Write(InputData.Length);
                w.Write(ChatMessages.Length);

                foreach (var input in InputData)
                {
                    w.Write(input);
                }
                foreach (var msg in ChatMessages)
                {
                    w.Write(msg.Time);
                    w.Write(msg.Player);
                    w.Write(msg.Message.Length);
                    var buffer = new byte[(msg.Message.Length + 1) * 2];
                    Buffer.BlockCopy(msg.Message.ToCharArray(), 0, buffer, 0, msg.Message.Length * 2);
                    w.Write(buffer);
                }
            }
        }
    }
}
