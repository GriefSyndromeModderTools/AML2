using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.DirectX.ActorTransform
{
    public struct ActorImageRef
    {
        internal int A, B;
    }

    public struct ActorObject
    {
        public IntPtr RawPointer { get; }

        public ActorObject(IntPtr ptr)
        {
            RawPointer = ptr;
        }

        private IntPtr AnimationManager => Marshal.ReadIntPtr(RawPointer, 0xBC);
        private IntPtr CurrentFrameInfo => Marshal.ReadIntPtr(AnimationManager, 0x1C4);

        public bool IsActive
        {
            get => Marshal.ReadByte(RawPointer, 0x34) != 0;
        }

        public bool AnimationInfoAvailable
        {
            get => AnimationManager != IntPtr.Zero && CurrentFrameInfo != IntPtr.Zero;
        }

        public int Motion
        {
            get => AnimationInfoAvailable ? Marshal.ReadInt32(AnimationManager, 0xC) : 0;
        }

        public int KeyTake
        {
            get => AnimationInfoAvailable ? Marshal.ReadInt32(AnimationManager, 0x10) : 0;
        }

        public int FrameIndex
        {
            get => AnimationInfoAvailable ? Marshal.ReadInt32(AnimationManager, 0x14) : 0;
        }

        public ActorImageRef CurrentImage
        {
            get => new ActorImageRef
            {
                A = Marshal.ReadInt32(CurrentFrameInfo, 0x8),
                B = Marshal.ReadInt32(CurrentFrameInfo, 0xC),
            };
        }

        public IntPtr DestBuffer => CurrentFrameInfo + 0x10;
        public IntPtr SrcBuffer => CurrentFrameInfo + 0x88 + 0x38;

        private static readonly float[] _marshalFloat = new float[1];

        public float X
        {
            get
            {
                lock (_marshalFloat)
                {
                    Marshal.Copy(RawPointer + 0xF0, _marshalFloat, 0, 1);
                    return _marshalFloat[0];
                }
            }
        }

        public float Y
        {
            get
            {
                lock (_marshalFloat)
                {
                    Marshal.Copy(RawPointer + 0xF4, _marshalFloat, 0, 1);
                    return _marshalFloat[0];
                }
            }
        }
    }
}
