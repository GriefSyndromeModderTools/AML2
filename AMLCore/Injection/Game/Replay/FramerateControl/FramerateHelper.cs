using AMLCore.Injection.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Game.Replay.FramerateControl
{
    class FramerateHelper
    {
        private static float _Ratio = 1.0f;
        public static float Ratio
        {
            get
            {
                return _Ratio;
            }
            set
            {
                if (value < 1.0f)
                {
                    value = 1.0f;
                }
                _Ratio = value;
                //logic speed
                Marshal.WriteInt32(AddressHelper.Code(0x2AC51C), (int)Math.Floor(16.0f / value));
                Skip = 0;
            }
        }
        internal static float Skip = 0;
    }
}
