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
            rep.WriteChatMessage(1, 0, "Message 1");
            rep.WriteInputData(new byte[60 * 12 * 6], 0, 60 * 12);
            rep.WriteInputData(new byte[] { 0x10, 0, 0, 0, 0, 0 }, 0, 1);
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
            var block = CreateNewBlock(section);

            _stream.Seek(GetBlockFileOffset(block), SeekOrigin.Begin);
            var sectionu = (uint)section;
            _writer.Write(sectionu << 24);
            _stream.Seek(BlockSize - 4 - 1, SeekOrigin.Current);
            _writer.Write((byte)0);

            return section;
        }

        public void ResetSection(int section)
        {
            if (section >= _sectionTailBlockNumber.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(section));
            }
            _sectionTailBlockNumber[section] = -1;
            for (int i = 0; i < _sectionIndexForBlocks.Count; ++i)
            {
                if (_sectionIndexForBlocks[i] == section)
                {
                    _sectionIndexForBlocks[i] = -1;
                }
            }
        }

        public void AppendSection(int section, byte[] buffer, int offset, int length)
        {
            var block = _sectionTailBlockNumber[section];
            while (length > 0)
            {
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
            if (writtenMax < length)
            {
                _stream.Seek(GetBlockFileOffset(block) + currentCount, SeekOrigin.Begin);
                _stream.Write(buffer, offset, length);
                _stream.Seek(GetBlockFileOffset(block + 1) - 1, SeekOrigin.Begin);
                currentCount += (byte)length;
                _writer.Write(currentCount);
                return length;
            }
            else
            {
                _stream.Seek(GetBlockFileOffset(block) + currentCount, SeekOrigin.Begin);
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
            //Our empty message will cause error in gso so we decided to let it ignore the last msg.
            _writer.Write(_chatMessageOffset.Count - 1);
        }

        private void WriteLastChatMessage()
        {
            var pos = GetChatFileOffset(_chatMessageOffset.Count - 1);
            _stream.Seek(pos, SeekOrigin.Begin);
            _writer.Write(int.MaxValue);
            _writer.Write(0);
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
            _writer.Write((totalLen - 12) / 2 - 1);
        }

        private void MoveChatMessage(int index, int dist)
        {
            _stream.Seek(GetChatFileOffset(index), SeekOrigin.Begin);
            var t = _reader.ReadInt32();
            var p = _reader.ReadInt32();
            var l = _reader.ReadInt32() * 2 - dist;
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
            _stream.Read(_amlMoveBuffer, 0, ByteCountPerAMLMove);
            _stream.Seek(_amlDataEnd, SeekOrigin.Begin);
            _stream.Write(_amlMoveBuffer, 0, ByteCountPerAMLMove);
            _amlDataStart += ByteCountPerAMLMove;
            _amlDataEnd += ByteCountPerAMLMove;
            WriteLastChatMessage();

            //tail number
            for (int i = 0; i < _sectionTailBlockNumber.Count; ++i)
            {

                if (_sectionTailBlockNumber[i]  == -1)
                {
                }
                else if (_sectionTailBlockNumber[i] < AMLMoveCount)
                {
                    _sectionTailBlockNumber[i] += _sectionTailBlockNumber.Count - AMLMoveCount;
                }
                else
                {
                    _sectionTailBlockNumber[i] -= AMLMoveCount;
                }
            }

            //section id
            if (_sectionIndexForBlocks.Count > AMLMoveCount)
            {
                _sectionIndexForBlocks.AddRange(_sectionIndexForBlocks.Take(AMLMoveCount));
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
