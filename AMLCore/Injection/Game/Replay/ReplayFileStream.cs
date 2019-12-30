using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Replay
{
    internal class ReplayFileStream
    {
        private const int Magic = 0x50525347;
        private const int InitInputFrame = 60 * 10;
        private const int InitAmlOffset = 16 + InitInputFrame * 6 + ByteCountPerAMLMove * 2;

        private const int BlockSize = 256;
        private const int AMLMoveCount = 3;
        private const int FrameCountPerAMLMove = BlockSize * AMLMoveCount / 6;
        private const int ByteCountPerAMLMove = BlockSize * AMLMoveCount;
        private const int EmptyMessageLength = 250; //number of char, including '\0'
        private const int EmptyMessageCount = ByteCountPerAMLMove / (EmptyMessageLength * 2 + 12) + 1;

        //count. size is count*6. rep size is count*3
        private int _inputFrameCount;
        private int _usedInputFrameCount;
        //offset from chat message start (not from file start) (including the last one)
        private List<int> _chatMessageOffset = new List<int>();
        //index in _chatMessageOffset of the chat message that has been extended in last step
        //-1 means there is no message item with additional space (excluding the last empty one)
        private int _chatMovingCursor;
        //file offset of aml blocks
        private int _amlDataStart;
        //file offset of the end of aml blocks
        private int _amlDataEnd;

        //block number of the last block of each sections
        private List<int> _sectionTailBlockNumber = new List<int>();
        //block count for each section
        private List<int> _blockCountForSections = new List<int>();
        //section index for each block (-1 is empty)
        private List<int> _sectionIndexForBlocks = new List<int>();

        private Stream _stream;
        private BinaryWriter _writer;
        private BinaryReader _reader;

        //Block structure:
        //  int32: 8 byte section id+24 byte order index
        //  252 bytes data, or <=251 bytes data + empty + 1 byte data size (not including 4 byte block header)

        //For testing in VS imm window.
        public static void Test1()
        {
            var rep = new ReplayFileStream(@"E:\test.dat", 0x1234);
            rep.WriteChatMessage(60, 0, "吼吼");
            rep.WriteChatMessage(120, 1, new string('x', 255));
            rep.WriteInputData(new byte[60 * 12 * 6], 0, 60 * 12);
            rep.WriteInputData(new byte[] { 0x10, 0, 0, 0, 0, 0 }, 0, 1);
            var s1 = rep.CreateSection("aml.test");
            rep.AppendSection(s1, new byte[] { 1, 2, 3, 4 }, 0, 4);
            rep.WriteInputData(new byte[60 * 12 * 6], 0, 60 * 12);
            rep.MoveChatDataStep();
            var s2 = rep.CreateSection("aml.compressed");
            rep.AppendSection(s2, Enumerable.Range(0, 280).Select(x => (byte)x).ToArray(), 0, 280);
            rep.WriteInputData(new byte[60 * 24 * 6], 0, 60 * 24);
            for (int i = 0; i < 10; ++i)
            {
                rep.WriteChatMessage(i * 20 + 200, 0, i.ToString() + new string('!', 200));
            }
            rep.AppendSection(rep.CreateSection("aml.x2"), Enumerable.Range(0, 280).Select(x => (byte)x).ToArray(), 0, 251);
            rep.AppendSection(rep.CreateSection("aml.x3"), Enumerable.Range(0, 280).Select(x => (byte)x).ToArray(), 0, 252);
            rep.AppendSection(rep.CreateSection("aml.x4"), Enumerable.Range(0, 280).Select(x => (byte)x).ToArray(), 0, 253);
            rep.AppendSection(rep.CreateSection("aml.x5"), Enumerable.Range(0, 280).Select(x => (byte)x).ToArray(), 0, 254);
            rep.AppendSection(rep.CreateSection("aml.x6"), Enumerable.Range(0, 280).Select(x => (byte)x).ToArray(), 0, 255);
            rep.ResetSection(s2);
            rep.AppendSection(rep.CreateSection("aml.x7"), Enumerable.Range(0, 280).Select(x => (byte)x).ToArray(), 0, 256);
            rep.Close();

            var read = new ReplayFile(@"E:\test.dat");
        }

        public ReplayFileStream(string filename, int maxLap)
        {
            Init(filename, maxLap);
        }

        public int CreateSection(string id)
        {
            var idData = Encoding.UTF8.GetBytes(id);
            AppendSection(0, idData, 0, idData.Length);
            AppendSection(0, new byte[] { 0 }, 0, 1);

            var section = _sectionTailBlockNumber.Count;
            //We reserve section id 255 for future use.
            if (section >= 255)
            {
                throw new InvalidOperationException("section list full");
            }

            _sectionTailBlockNumber.Add(-1);
            _blockCountForSections.Add(0);
            CreateNewBlock(section);
            return section;
        }

        public void ResetSection(int section)
        {
            if (section >= _sectionTailBlockNumber.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(section));
            }
            var last = _sectionTailBlockNumber[section];
            for (int i = 0; i < _sectionIndexForBlocks.Count; ++i)
            {
                if (_sectionIndexForBlocks[i] == section)
                {
                    if (i == last)
                    {
                        _stream.Seek(GetBlockFileOffset(i), SeekOrigin.Begin);
                        _writer.Write(((uint)section) << 24);
                        _stream.Seek(251, SeekOrigin.Current);
                        _writer.Write((byte)0);
                    }
                    else
                    {
                        _sectionIndexForBlocks[i] = -1;
                        _stream.Seek(GetBlockFileOffset(i), SeekOrigin.Begin);
                        _writer.Write(0x00FFFFFF);
                    }
                }
            }
        }

        public void AppendSection(int section, byte[] buffer, int offset, int length)
        {
            var block = _sectionTailBlockNumber[section];
            while (length > 0)
            {
                //Note that written is length+1 if all data is successfully written.
                var written = AppendBlockData(block, buffer, offset, length);
                offset += written;
                length -= written;
                if (length >= 0) //Need an empty block even we already write all data.
                {
                    block = CreateNewBlock(section);
                }
            }
        }

        public void WriteInputData(byte[] buffer, int startFrameIndex, int frameCount)
        {
            while (_usedInputFrameCount + frameCount > _inputFrameCount)
            {
                FinishMoveChatData();
            }
            _stream.Seek(16 + _usedInputFrameCount * 6, SeekOrigin.Begin);
            _writer.Write(buffer, startFrameIndex * 6, frameCount * 6);
            _usedInputFrameCount += frameCount;
        }

        public void WriteChatMessage(int frame, int player, string msg)
        {
            var strData = Encoding.Unicode.GetBytes(msg);
            var totalLength = 12 + strData.Length + 2;
            var writeStart = _chatMessageOffset[_chatMessageOffset.Count - 1];
            var writeEnd = writeStart + totalLength;
            _chatMessageOffset.Add(writeEnd);
            if (GetChatFileOffset(0) + writeEnd + 14 > _amlDataStart) //Need one more empty message
            {
                MoveAMLDataStep();
            }

            //We are writing to second last
            _stream.Seek(GetChatFileOffset(_chatMessageOffset.Count - 2), SeekOrigin.Begin);
            _writer.Write(frame);
            _writer.Write(player);
            _writer.Write(msg.Length);
            _writer.Write(strData);
            _writer.Write((ushort)0);
            WriteLastChatMessage();
            UpdateRepChatSize();
        }

        public void Flush()
        {
            _stream.Flush();
        }

        public void Close()
        {
            Flush();
            _stream.Close();
        }

        private int AppendBlockData(int block, byte[] buffer, int offset, int length)
        {
            _stream.Seek(GetBlockFileOffset(block + 1) - 1, SeekOrigin.Begin);
            var currentCount = _reader.ReadByte();
            var writtenMax = BlockSize - 4 - currentCount;
            if (writtenMax > length)
            {
                _stream.Seek(GetBlockFileOffset(block) + 4 + currentCount, SeekOrigin.Begin);
                _stream.Write(buffer, offset, length);
                _stream.Seek(GetBlockFileOffset(block + 1) - 1, SeekOrigin.Begin);
                currentCount += (byte)length;
                _writer.Write(currentCount);
                return length + 1;
            }
            else
            {
                _stream.Seek(GetBlockFileOffset(block) + 4 + currentCount, SeekOrigin.Begin);
                _stream.Write(buffer, offset, writtenMax);
                return writtenMax;
            }
        }

        private int CreateNewBlock(int section)
        {
            var ret = _sectionIndexForBlocks.FindIndex(ii => ii == -1);
            if (ret == -1)
            {
                ret = _sectionIndexForBlocks.Count;
                _sectionIndexForBlocks.Add(-1);
                _amlDataEnd += BlockSize;
                _stream.SetLength(_amlDataEnd);
            }

            _sectionIndexForBlocks[ret] = section;
            _blockCountForSections[section] += 1;
            _sectionTailBlockNumber[section] = ret;

            //Init new block.
            _stream.Seek(GetBlockFileOffset(ret), SeekOrigin.Begin);
            var sectionu = (uint)section;
            var header = sectionu << 24 | (uint)(_blockCountForSections[section] - 1);
            _writer.Write(header);
            _stream.Seek(251, SeekOrigin.Current);
            _writer.Write((byte)0);

            return ret;
        }

        private void Init(string filename, int maxLap)
        {
            _stream = File.Create(filename);
            _writer = new BinaryWriter(_stream);
            _reader = new BinaryReader(_stream);

            _writer.Write(Magic);
            _writer.Write(maxLap);
            _writer.Write(InitInputFrame * 3);
            _writer.Write(0);

            _inputFrameCount = InitInputFrame;
            _usedInputFrameCount = 0;
            _chatMessageOffset.Add(0);
            _chatMovingCursor = -1;

            _amlDataStart = InitAmlOffset;
            _amlDataEnd = _amlDataStart + BlockSize; //Initially we have section 0 (empty).

            _sectionIndexForBlocks.Add(0);
            _sectionTailBlockNumber.Add(0);
            _blockCountForSections.Add(1);

            _stream.SetLength(_amlDataEnd);
            //The first section is all zero

            WriteLastChatMessage();
        }

        private int GetChatFileOffset(int index)
        {
            return 16 + _inputFrameCount * 6 + _chatMessageOffset[index];
        }

        private int GetBlockFileOffset(int n)
        {
            return _amlDataStart + BlockSize * n;
        }

        private void UpdateRepInputSize()
        {
            _stream.Seek(8, SeekOrigin.Begin);
            _writer.Write(_inputFrameCount * 3);
        }

        private void UpdateRepChatSize()
        {
            _stream.Seek(12, SeekOrigin.Begin);
            if (_chatMovingCursor == -1)
            {
                //Our last message is too long for gso. Don't include it.
                _writer.Write(_chatMessageOffset.Count - 1);
            }
            else
            {
                _writer.Write(_chatMessageOffset.Count - 1 + EmptyMessageCount);
            }
        }

        private void WriteLastChatMessage()
        {
            var pos = GetChatFileOffset(_chatMessageOffset.Count - 1);
            _stream.Seek(pos, SeekOrigin.Begin);
            _writer.Write(int.MaxValue);
            _writer.Write(3); //message with player id=3 won't be displayed
            var len = _amlDataStart - pos - 12;
            if (len < 2)
            {
                throw new Exception("RepRecorder internal error");
            }
            _writer.Write(len / 2 - 1);
            _writer.Write('\0');
        }

        private void UpdateChatMessageSize(int index)
        {
            _stream.Seek(GetChatFileOffset(index) + 8, SeekOrigin.Begin);
            var totalLen = _chatMessageOffset[index + 1] - _chatMessageOffset[index];
            if (totalLen < 12)
            {
                throw new Exception("RepRecorder internal error");
            }
            //_writer.Write((totalLen - 12) / 2 - 1);

            var dataLen = _reader.ReadInt32();
            _stream.Seek(dataLen * 2 + 2, SeekOrigin.Current);
            var emptyLen = totalLen - 12 - dataLen * 2 - 2;
            var remainingCount = EmptyMessageCount;
            while (remainingCount > 0)
            {
                var l = EmptyMessageLength;
                if (remainingCount == 2)
                {
                    l = (emptyLen - 24) / 4;
                }
                else if (remainingCount == 1)
                {
                    l = (emptyLen - 12) / 2;
                }
                emptyLen -= 12 + l * 2;
                remainingCount -= 1;
                _writer.Write(int.MaxValue);
                _writer.Write(3);
                _writer.Write(l - 1);
                _writer.Write((ushort)0);
                _stream.Seek(l * 2 - 2, SeekOrigin.Current);
            }
        }

        private void MoveChatMessage(int index, int dist)
        {
            _stream.Seek(GetChatFileOffset(index), SeekOrigin.Begin);
            var t = _reader.ReadInt32();
            var p = _reader.ReadInt32();
            var l = _reader.ReadInt32() * 2;
            if (l < 0)
            {
                throw new Exception("RepRecorder internal error");
            }
            var data = _reader.ReadBytes(l);
            _chatMessageOffset[index] += dist;
            _stream.Seek(GetChatFileOffset(index), SeekOrigin.Begin);
            _writer.Write(t);
            _writer.Write(p);
            _writer.Write(l / 2);
            _writer.Write(data);
            _writer.Write((ushort)0);
            if (index != 0)
            {
                UpdateChatMessageSize(index - 1);
            }
        }

        private byte[] _amlMoveBuffer = new byte[ByteCountPerAMLMove];

        //This function will give 256*3 (=768) bytes space (3 blocks per move)
        private void MoveAMLDataStep()
        {
            _stream.Seek(_amlDataStart, SeekOrigin.Begin);
            var moved = _stream.Read(_amlMoveBuffer, 0, ByteCountPerAMLMove);
            if (_sectionIndexForBlocks.Count <= AMLMoveCount)
            {
                _amlDataStart += ByteCountPerAMLMove;
                _amlDataEnd += ByteCountPerAMLMove;
                _stream.Seek(_amlDataStart, SeekOrigin.Begin);
                _stream.Write(_amlMoveBuffer, 0, moved);
            }
            else
            {
                _stream.Seek(_amlDataEnd, SeekOrigin.Begin);
                _stream.Write(_amlMoveBuffer, 0, ByteCountPerAMLMove);
                _amlDataStart += ByteCountPerAMLMove;
                _amlDataEnd += ByteCountPerAMLMove;
            }
            WriteLastChatMessage();

            if (_sectionIndexForBlocks.Count > AMLMoveCount)
            {
                //tail number
                for (int i = 0; i < _sectionTailBlockNumber.Count; ++i)
                {
                    if (_sectionTailBlockNumber[i] == -1)
                    {
                    }
                    else if (_sectionTailBlockNumber[i] < AMLMoveCount)
                    {
                        _sectionTailBlockNumber[i] += _sectionIndexForBlocks.Count - AMLMoveCount;
                    }
                    else
                    {
                        _sectionTailBlockNumber[i] -= AMLMoveCount;
                    }
                }

                //section id
                _sectionIndexForBlocks.AddRange(_sectionIndexForBlocks.Take(AMLMoveCount).ToArray());
                _sectionIndexForBlocks.RemoveRange(0, AMLMoveCount);
            }
        }

        private byte[] _emptyBuffer = new byte[ByteCountPerAMLMove];

        private void AppendEmptyFrames(int count)
        {
            _stream.Seek(16 + _inputFrameCount * 6, SeekOrigin.Begin);
            if (_emptyBuffer.Length < count * 6)
            {
                _emptyBuffer = new byte[count * 6];
            }
            _writer.Write(_emptyBuffer, 0, count * 6);
            _inputFrameCount += count;
        }

        //Move the space of 256*3 byte to backwards
        private bool MoveChatDataStep()
        {
            if (_chatMessageOffset.Count == 1)
            {
                AppendEmptyFrames(FrameCountPerAMLMove);
                UpdateRepInputSize();
                WriteLastChatMessage();
                return false;
            }

            //We have some data. Need to move
            if (_chatMovingCursor == -1)
            {
                //Before we move, we need to check whether the empty one has enough space
                var lastPos = GetChatFileOffset(_chatMessageOffset.Count - 1);
                if (lastPos + 12 + 2 + ByteCountPerAMLMove > _amlDataStart)
                {
                    MoveAMLDataStep();
                }

                //Move from the second last one (the last one excluding the empty one)
                _chatMovingCursor = _chatMessageOffset.Count - 2;
                _chatMessageOffset[_chatMessageOffset.Count - 1] += ByteCountPerAMLMove;
                WriteLastChatMessage();
                UpdateChatMessageSize(_chatMovingCursor);
                UpdateRepChatSize();
                return true;
            }

            MoveChatMessage(_chatMovingCursor, ByteCountPerAMLMove);
            _chatMovingCursor -= 1;
            if (_chatMovingCursor == -1)
            {
                var reduce = _chatMessageOffset[0];
                for (int i = 0; i < _chatMessageOffset.Count; ++i)
                {
                    _chatMessageOffset[i] -= reduce;
                }
                AppendEmptyFrames(FrameCountPerAMLMove);
                UpdateRepInputSize();
                UpdateRepChatSize();
                return false;
            }
            return true;
        }

        private void FinishMoveChatData()
        {
            while (MoveChatDataStep()) {}
        }

        //called every several frames
        public void MoveAllOnce()
        {
            var lastChatLength = _amlDataStart - GetChatFileOffset(_chatMessageOffset.Count - 1);
            if (lastChatLength < 768 * 10)
            {
                MoveAMLDataStep();
            }
            if (_inputFrameCount < _usedInputFrameCount + 3600)
            {
                MoveChatDataStep();
            }
        }
    }
}
