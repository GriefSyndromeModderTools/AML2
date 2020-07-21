using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Native
{
    public abstract class ModifyRegisterInjection
    {
        public struct RegisterProfile
        {
            public IntPtr EAX, ECX, EBX, EDX, ESI, EDI, ESP, EBP;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct Union
        {
            [FieldOffset(0)]
            public IntPtr[] Pointers;
            [FieldOffset(0)]
            public RegisterProfile[] Registers;
        }

        public ModifyRegisterInjection(uint offset, int len)
        {
            Inject(AddressHelper.Code(offset), len);
        }

        public ModifyRegisterInjection(IntPtr addr, int len)
        {
            Inject(addr, len);
        }

        protected abstract void Triggered(ref RegisterProfile registers);

        private void NativeCallback(IntPtr ptr)
        {
            Union union = new Union { Pointers = new IntPtr[8] };
            Marshal.Copy(ptr, union.Pointers, 0, 8);
            union.Registers[0].ESP += 4; //because we pushed ebp before esp
            Triggered(ref union.Registers[0]);
            Marshal.Copy(union.Pointers, 0, ptr, 8);
        }

        private void Inject(IntPtr addr, int len)
        {
            CoreLoggers.Injection.Info("inject code with {0} at 0x{1}",
                this.GetType().FullName, addr.ToInt32().ToString("X8"));
            OverlapCheck.Add(addr, len);

            var index = NativeEntrance.NextIndex();
            NativeEntrance.Register(index, NativeCallback);

            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    IntPtr pJumpForward = AssemblyCodeStorage.AllocateIndirect();
                    IntPtr pJumpBackward = AssemblyCodeStorage.AllocateIndirect();

                    bw.Write(new byte[]
                    {
                        //push ebp-eax
                        0x55, 0x54, 0x57, 0x56, 0x52, 0x53, 0x51, 0x50,

                        //lea eax, [esp]
                        0x8D, 0x04, 0x24,

                        //push eax
                        0x50,
                        //push index
                        0x68,
                    });
                    bw.Write(index);
                    bw.Write(new byte[]
                    {
                        //mov eax, xxxxxxxx
                        0xB8,
                    });
                    bw.Write(NativeEntrance.EntrancePtr.ToInt32());
                    bw.Write(new byte[]
                    {
                        //call eax
                        0xFF, 0xD0,

                        //pop eax-edi, ignore esp, ebp
                        0x58, 0x59, 0x5B, 0x5A, 0x5E, 0x5F, 0x83, 0xC4, 0x08,
                    });

                    byte[] moved = new byte[len];
                    Marshal.Copy(addr, moved, 0, len);
                    bw.Write(moved);

                    bw.Write((byte)0xFF);
                    bw.Write((byte)0x25);
                    bw.Write(pJumpBackward.ToInt32());

                    var dest = AssemblyCodeStorage.WriteCode(ms.ToArray());
                    AssemblyCodeStorage.WriteIndirect(pJumpForward, dest);
                    AssemblyCodeStorage.WriteIndirect(pJumpBackward, addr + len);

                    using (new ReadWriteProtect(addr, len))
                    {
                        Marshal.WriteByte(addr, 0, 0xFF);
                        Marshal.WriteByte(addr, 1, 0x25);
                        Marshal.WriteInt32(addr, 2, pJumpForward.ToInt32());
                    }
                }
            }
        }
    }
}
