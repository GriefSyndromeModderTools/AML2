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
        private readonly IntPtr _ptr;

        public ActorObject(IntPtr ptr)
        {
            _ptr = ptr;
        }

        private IntPtr AnimationManager => Marshal.ReadIntPtr(_ptr, 0xBC);
        private IntPtr CurrentFrameInfo => Marshal.ReadIntPtr(AnimationManager, 0x1C4);

        public bool IsActive
        {
            get => Marshal.ReadByte(_ptr, 0x34) != 0;
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
    }
}
