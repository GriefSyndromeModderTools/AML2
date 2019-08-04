using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.Input
{
    public class ReadOnlyInputHandler : IInputHandler
    {
        public class SingleInput
        {
            internal bool[] Buffer;
            internal int Offset;

            internal void Update()
            {
                X = UpdateDir(GetDir(Buffer[Offset + 2], Buffer[Offset + 3]), GetDir(Buffer[Offset + 27 + 2], Buffer[Offset + 27 + 3]), X);
                Y = UpdateDir(GetDir(Buffer[Offset + 0], Buffer[Offset + 1]), GetDir(Buffer[Offset + 27 + 0], Buffer[Offset + 27 + 1]), Y);
                B0 = UpdateChannel(Buffer[Offset + 4], Buffer[Offset + 27 + 4], B0);
                B1 = UpdateChannel(Buffer[Offset + 5], Buffer[Offset + 27 + 5], B1);
                B2 = UpdateChannel(Buffer[Offset + 6], Buffer[Offset + 27 + 6], B2);
                B3 = UpdateChannel(Buffer[Offset + 7], Buffer[Offset + 27 + 7], B3);
                B4 = UpdateChannel(Buffer[Offset + 8], Buffer[Offset + 27 + 8], B4);
            }

            public int B0 { get; private set; } = -65536;
            public int B1 { get; private set; } = -65536;
            public int B2 { get; private set; } = -65536;
            public int B3 { get; private set; } = -65536;
            public int B4 { get; private set; } = -65536;
            public int X { get; private set; } = 0;
            public int Y { get; private set; } = 0;

            private static int UpdateChannel(bool last, bool current, int lastInt)
            {
                if (last == current)
                {
                    if (lastInt == -65536 || lastInt == 65535) return lastInt;
                    return lastInt + (current ? 1 : -1);
                }
                else
                {
                    return current ? 1 : -1;
                }
            }

            private static int GetDir(bool neg, bool pos)
            {
                return pos ? 1 : neg ? -1 : 0;
            }

            private static int UpdateDir(int lastDir, int currentDir, int lastInt)
            {
                if (currentDir == 0) return 0;
                if (lastDir == currentDir)
                {
                    if (lastInt == -65536 || lastInt == 65535) return lastInt;
                    return lastInt + currentDir;
                }
                else
                {
                    return currentDir;
                }
            }
        }

        public class MultiInput
        {
            internal SingleInput[] Single;

            internal void Update()
            {
                B0 = UpdateChannel(Single[0].B0, Single[1].B0, Single[2].B0);
                B1 = UpdateChannel(Single[0].B1, Single[1].B1, Single[2].B1);
                B2 = UpdateChannel(Single[0].B2, Single[1].B2, Single[2].B2);
                B3 = UpdateChannel(Single[0].B3, Single[1].B3, Single[2].B3);
                B4 = UpdateChannel(Single[0].B4, Single[1].B4, Single[2].B4);
                X = UpdateDir(Single[0].X, Single[1].X, Single[2].X);
                Y = UpdateDir(Single[0].Y, Single[1].Y, Single[2].Y);
            }

            public int B0 { get; private set; }
            public int B1 { get; private set; }
            public int B2 { get; private set; }
            public int B3 { get; private set; }
            public int B4 { get; private set; }
            public int X { get; private set; }
            public int Y { get; private set; }

            private static int UpdateChannel(int p0, int p1, int p2)
            {
                var ret = Math.Max(p0, Math.Max(p1, p2));
                if (ret > 0)
                {
                    if (p0 > 0 && p0 < ret) ret = p0;
                    if (p1 > 0 && p1 < ret) ret = p1;
                    if (p2 > 0 && p2 < ret) ret = p2;
                }
                return ret;
            }

            private static int UpdateDir(int p0, int p1, int p2)
            {
                var ret = p0;
                if (Math.Abs(p1) > Math.Abs(ret))
                {
                    ret = p1;
                }
                if (Math.Abs(p2) > Math.Abs(ret))
                {
                    ret = p2;
                }
                return ret;
            }
        }

        private bool[] _buffer;
        private SingleInput[] _single;
        public ReadOnlyCollection<SingleInput> Input { get; private set; }
        public MultiInput InputAll { get; private set; }
        private static ReadOnlyInputHandler _instance = new ReadOnlyInputHandler();

        private ReadOnlyInputHandler()
        {
            _buffer = new bool[27 * 2];
            _single = new SingleInput[]
            {
                new SingleInput { Buffer = _buffer, Offset = 0 },
                new SingleInput { Buffer = _buffer, Offset = 9 },
                new SingleInput { Buffer = _buffer, Offset = 18 },
            };
            Input = new ReadOnlyCollection<SingleInput>(_single);
            InputAll = new MultiInput { Single = _single };

            KeyConfigRedirect.Redirect();
            InputManager.RegisterHandler(this, InputHandlerType.GameInput);
        }

        public static ReadOnlyInputHandler Get()
        {
            return _instance;
        }

        public bool HandleInput(IntPtr ptr)
        {
            for (int i = 0; i < 27; ++i)
            {
                _buffer[i] = _buffer[i + 27];
            }
            for (int i = 0; i < 27; ++i)
            {
                _buffer[i + 27] = Marshal.ReadByte(ptr, KeyConfigRedirect.GetKeyIndex(i)) == 0x80;
            }
            _single[0].Update();
            _single[1].Update();
            _single[2].Update();
            InputAll.Update();
            return false;
        }
    }
}
