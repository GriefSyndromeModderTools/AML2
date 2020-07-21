using AMLCore.Injection.Native;
using AMLCore.Logging;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Debugging
{
    class InvalidSetMotionLog : IEntryPointLoad
    {
        private static Logger _logger = new Logger("SetMotion");
        private static HashSet<int> _animations = new HashSet<int>();
        private static HashSet<IntPtr> _actors = new HashSet<IntPtr>();

        public void Run()
        {
            //new InjectSetMotion();
            //foreach (var id in File.ReadAllLines(@"E:\allAnimations.txt"))
            //{
            //    _animations.Add(int.Parse(id));
            //}
            //_logger.Info($"{_animations.Count} animations known.");
        }

        private class InjectSetMotion : ModifyRegisterInjection
        {
            public InjectSetMotion() : base(0x11A23, 6)
            {
            }

            protected override void Triggered(ref RegisterProfile registers)
            {
                var actorPtr = registers.ECX;
                var motion = Marshal.ReadInt32(registers.EBP + 8);
                if (!_actors.Add(actorPtr) && _animations.Contains(motion))
                {
                    return;
                }
                var key = Marshal.ReadInt32(registers.EBP + 12);
                _logger.Info($"0x{actorPtr:X8} = ({motion}, {key}){(_animations.Contains(motion) ? "new actor" : "unknown animation")}");
            }
        }
    }
}
