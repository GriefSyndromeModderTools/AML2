using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.DirectX.ActorTransform
{
    internal class TestTransformHandler : IEntryPointLoad
    {
        public void Run()
        {
            //ActorTransformManager.RegisterHandler(200, new Handler2());
        }

        private class Handler : IActorTransformHandler
        {
            private static IntPtr _src = Marshal.AllocHGlobal(4 * 3);
            private static IntPtr _dest = Marshal.AllocHGlobal(4 * 4);
            private static float[] _marshal = new float[1];

            static Handler()
            {
                Marshal.WriteInt32(_src, 0, 0);
                Marshal.WriteInt32(_src, 4, 0);
                Marshal.WriteInt32(_src, 8, 0);
            }

            public void Handle(IActorRenderer renderer, ActorObject actor, ActorTransformMatrix matrix)
            {
                renderer.DrawActor(actor);

                matrix.TransformBuffer(_dest, 4 * 4, _src, 4 * 3, 1);
                Marshal.Copy(_dest + 4, _marshal, 0, 1);
                var y0 = _marshal[0];
                var buffer = actor.DestBuffer;

                for (int i = 0; i < 4; ++i)
                {
                    Marshal.Copy(buffer + 0x1C * i + 4, _marshal, 0, 1);
                    _marshal[0] = _marshal[0] * 0.5f + y0 * 0.5f;
                    Marshal.Copy(_marshal, 0, buffer + 0x1C * i + 4, 1);
                }
            }
        }

        private class Handler2 : IActorTransformHandler
        {
            private static IntPtr _src = Marshal.AllocHGlobal(4 * 3);
            private static IntPtr _dest = Marshal.AllocHGlobal(4 * 4);
            private static float[] _marshal = new float[1];

            static Handler2()
            {
                Marshal.WriteInt32(_src, 0, 0);
                Marshal.WriteInt32(_src, 4, 0);
                Marshal.WriteInt32(_src, 8, 0);
            }

            public void Handle(IActorRenderer renderer, ActorObject actor, ActorTransformMatrix matrix)
            {
                var buffer = actor.DestBuffer;
                for (int i = 0; i < 4; ++i)
                {
                    Marshal.WriteByte(buffer + 0x1C * i + 4 * 4 + 3, (byte)(255 - (6 - 0) * 40));
                }
                for (int j = 0; j < 7; ++j)
                {
                    renderer.DrawActor(actor);

                    for (int i = 0; i < 4; ++i)
                    {
                        Marshal.Copy(buffer + 0x1C * i + 0, _marshal, 0, 1);
                        _marshal[0] += 15;
                        Marshal.Copy(_marshal, 0, buffer + 0x1C * i + 0, 1);
                        Marshal.WriteByte(buffer + 0x1C * i + 4 * 4 + 3, (byte)(255 - (6 - j + 1) * 30));
                    }
                }
            }
        }
    }
}
