using AMLCore.Internal;
using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Replay
{
    class ReplayRecorder
    {
        private IniFile _Config = new IniFile("Core");
        private string GetFolder()
        {
            return _Config.Read("Replay", "Folder", "aml/replay");
        }
        private string GetFilenameFormat()
        {
            return _Config.Read("Replay", "Filename", "yyyy-MM/yyMMddHHmmss");
        }
        private string GetFileName()
        {
            return FileNameParser.Parse(GetFilenameFormat());
        }
        private bool UseCompression()
        {
            var mode = _Config.Read("Replay", "Compression", "compatible");
            if (mode == "compatible")
            {
                return PluginLoader.ContainsFunctionalMods(false);
            }
            else if (mode == "always")
            {
                return true;
            }
            return false;
        }

        internal ReplayFileStream _stream;
        private const int BufferredFrameCount = 10;
        private const int CompressedFrameCount = 1000 / BufferredFrameCount * BufferredFrameCount;
        private byte[] _buffer = new byte[6 * BufferredFrameCount];
        private int _bufferFrame = 0;
        private bool _compressed;
        private int _compressedRawSection, _compressedBlockSection;
        private byte[] _compressedBuffer;
        private int _compressedFrame = 0;
        private int _totalFrames = 0;

        public ReplayRecorder()
        {
            _compressed = UseCompression();

            var path = PathHelper.GetPath(GetFolder());
            var file = Path.Combine(path, GetFileName()) + ".repx";
            var dir = Path.GetDirectoryName(file);
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                _stream = new ReplayFileStream(file, ReplayRecorderEntry.MaxLap);
                CoreLoggers.Replay.Info("replay file created at {0}", file);
                if (_compressed)
                {
                    _compressedBuffer = new byte[CompressedFrameCount * 6];
                    _compressedRawSection = _stream.CreateSection("aml.rep.raw");
                    _compressedBlockSection = _stream.CreateSection("aml.rep.blocks");
                    CoreLoggers.Replay.Info("compression mode enabled");
                }
            }
            catch
            {
                CoreLoggers.Replay.Error("cannot create replay file {0}", file);
            }
        }

        public void WriteChat(int player, string msg)
        {
            _stream.WriteChatMessage(_totalFrames, player, msg);
        }

        public void WriteFrame(bool[] buffer, int offset)
        {
            WriteFrame(buffer, offset, _buffer, _bufferFrame * 6);
            _bufferFrame += 1;
            _totalFrames += 1;
            if (_bufferFrame == BufferredFrameCount)
            {
                ClearBuffer();
            }
        }

        private void ClearBuffer()
        {
            if (_stream != null)
            {
                if (_compressed)
                {
                    _stream.AppendSection(_compressedRawSection, _buffer, 0, BufferredFrameCount * 6);

                    Array.Copy(_buffer, 0, _compressedBuffer, _compressedFrame * 6, BufferredFrameCount * 6);
                    _compressedFrame += BufferredFrameCount;
                    if (_compressedFrame == CompressedFrameCount)
                    {
                        DoCompress();
                        _compressedFrame = 0;
                    }
                }
                else
                {
                    _stream.WriteInputData(_buffer, 0, BufferredFrameCount);
                }
            }
            _bufferFrame = 0;
        }

        private static byte[] CompressData(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                using (var compress = new DeflateStream(ms, CompressionMode.Compress))
                {
                    using (var ms2 = new MemoryStream(data))
                    {
                        ms2.CopyTo(compress);
                        compress.Close();
                        return ms.ToArray();
                    }
                }
            }
        }

        private void DoCompress()
        {
            _stream.ResetSection(_compressedRawSection);
            var compressedData = CompressData(_compressedBuffer);
            _stream.AppendSection(_compressedBlockSection, BitConverter.GetBytes(compressedData.Length), 0, 4);
            _stream.AppendSection(_compressedBlockSection, compressedData, 0, compressedData.Length);
        }

        private void WriteFrame(bool[] buffer, int offset, byte[] output, int outputOffset)
        {
            WritePlayerToBuffer(buffer, offset, output, outputOffset);
            WritePlayerToBuffer(buffer, offset + 9, output, outputOffset + 2);
            WritePlayerToBuffer(buffer, offset + 18, output, outputOffset + 4);
        }

        private void WritePlayerToBuffer(bool[] input, int inputOffset, byte[] buffer, int bufferOffset)
        {
            byte m = 0;
            if (input[inputOffset + 0]) m += 1;
            if (input[inputOffset + 1]) m += 2;
            if (input[inputOffset + 2]) m += 4;
            if (input[inputOffset + 3]) m += 8;
            if (input[inputOffset + 4]) m += 16;
            if (input[inputOffset + 5]) m += 32;
            if (input[inputOffset + 6]) m += 64;
            if (input[inputOffset + 7]) m += 128;
            buffer[bufferOffset] = m;
            buffer[bufferOffset + 1] = 0;
            if (input[inputOffset + 8]) buffer[bufferOffset + 1] += 1;
        }
    }
}
