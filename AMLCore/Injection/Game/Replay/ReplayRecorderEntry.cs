using AMLCore.Injection.Engine.Input;
using AMLCore.Injection.Game.SaveData;
using AMLCore.Internal;
using AMLCore.Logging;
using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Game.Replay
{
    class ReplayRecorderEntry : IEntryPointPostload, IInputHandler
    {
        private Stream _Output;
        private static int _MaxLap = 0;

        private static IniFile _Config = new IniFile("Core");
        private static string GetFolder()
        {
            return _Config.Read("Replay", "Folder", "aml/replay");
        }
        private static string GetFilenameFormat()
        {
            return _Config.Read("Replay", "Filename", "yyMMddHHmmss");
        }
        private static string GetFileName()
        {
            return FileNameParser.Parse(GetFilenameFormat());
        }
        private static bool UseCompression()
        {
            var mode = _Config.Read("Replay", "Compression", "compatible");
            if (mode == "compatible")
            {
                return PluginLoader.ContainsFunctionalMods();
            }
            else if (mode == "always")
            {
                return true;
            }
            return false;
        }

        public void Run()
        {
            SaveDataHelper.ModifySaveData += ProcessSaveData;
            KeyConfigRedirect.Redirect();
            InputManager.RegisterHandler(this, InputHandlerType.RawInput);
        }

        private void CreateFile()
        {
            var path = PathHelper.GetPath(GetFolder());
            var file = Path.Combine(path, GetFileName()) + ".rep";
            var dir = Path.GetDirectoryName(file);
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                _Output = File.Open(file, FileMode.CreateNew, FileAccess.Write);
                CoreLoggers.Replay.Info("replay file created at {0}", file);
            }
            catch
            {
                CoreLoggers.Replay.Error("cannot create replay file {0}", file);
                _Output = Stream.Null;
            }
            _Writer = new BinaryWriter(_Output);
        }

        private static void ProcessSaveData(GSDataFile.CompoundType data)
        {
            try
            {
                data["lastPlayLap"] = 0;
                _MaxLap = (int)data["loopNum"];
                var results = (GSDataFile.CompoundType)data["result"];
                for (int i = 0; i < 20; ++i)
                {
                    results[i] = true;
                }
            }
            catch (Exception e)
            {
                CoreLoggers.Replay.Error("cannot modify save data: " + e.ToString());
            }
        }

        private BinaryWriter _Writer;
        private const int _BufferLength = 1;
        private bool[] _Buffer = new bool[27 * _BufferLength];
        private int _BufferFrameCount = 0, _FrameCount = 0;

        public bool HandleInput(IntPtr ptr)
        {
            for (int i = 0; i < 27; ++i)
            {
                _Buffer[_BufferFrameCount * 27 + i] =
                    Marshal.ReadByte(ptr, KeyConfigRedirect.GetKeyIndex(i)) == 0x80;
            }
            if (_FrameCount == 0)
            {
                CreateFile();
                WriteHeader();
            }
            _BufferFrameCount += 1;
            _FrameCount += 1;
            if (_BufferFrameCount == _BufferLength)
            {
                ClearBuffer();
            }
            return false;
        }

        private void WriteHeader()
        {
            _Writer.Write(0x50525347);
            _Writer.Write(_MaxLap);
            _Writer.Write(0);
            _Writer.Write(0);
        }

        private void ClearBuffer()
        {
            _Output.Seek(8, SeekOrigin.Begin);
            _Writer.Write(_FrameCount * 3);
            _Output.Seek(16 + (_FrameCount - _BufferFrameCount) * 6, SeekOrigin.Begin);
            for (int i = 0; i < _BufferFrameCount; ++i)
            {
                WriteFrame(_Buffer, 3 * 9 * i);
            }
            _BufferFrameCount = 0;
            _Output.Flush();
        }

        private void WriteFrame(bool[] buffer, int offset)
        {
            WritePlayer(buffer, offset);
            WritePlayer(buffer, offset + 9);
            WritePlayer(buffer, offset + 18);
        }

        private void WritePlayer(bool[] buffer, int offset)
        {
            short ret = 0;
            if (buffer[offset + 0]) ret += 1;
            if (buffer[offset + 1]) ret += 2;
            if (buffer[offset + 2]) ret += 4;
            if (buffer[offset + 3]) ret += 8;
            if (buffer[offset + 4]) ret += 16;
            if (buffer[offset + 5]) ret += 32;
            if (buffer[offset + 6]) ret += 64;
            if (buffer[offset + 7]) ret += 128;
            if (buffer[offset + 8]) ret += 256;
            _Writer.Write(ret);
        }
    }
}
