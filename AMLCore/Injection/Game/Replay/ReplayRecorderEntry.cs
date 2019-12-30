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
        public static int MaxLap;

        private ReplayRecorder _recorder;
        private bool[] _buffer = new bool[27];

        public void Run()
        {
            SaveDataHelper.ModifySaveData += ProcessSaveData;
            KeyConfigRedirect.Redirect();
            InputManager.RegisterHandler(this, InputHandlerType.RawInput);
        }

        private static void ProcessSaveData(GSDataFile.CompoundType data)
        {
            try
            {
                data["lastPlayLap"] = 0;
                MaxLap = (int)data["loopNum"];
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

        public bool HandleInput(IntPtr ptr)
        {
            for (int i = 0; i < 27; ++i)
            {
                _buffer[i] = Marshal.ReadByte(ptr, KeyConfigRedirect.GetKeyIndex(i)) == 0x80;
            }
            if (_recorder == null)
            {
                _recorder = new ReplayRecorder();
            }
            _recorder.WriteFrame(_buffer, 0);
            return false;
        }
    }
}
