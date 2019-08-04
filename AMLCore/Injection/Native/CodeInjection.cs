using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Native
{
    public abstract class CodeInjection : AbstractNativeInjection
    {
        public CodeInjection(uint offset, int len)
        {
            Init(AddressHelper.Code(offset), len);
        }

        public CodeInjection(IntPtr addr, int len)
        {
            Init(addr, len);
        }

        private void Init(IntPtr addr, int len)
        {
            if (len < 6)
            {
                throw new ArgumentOutOfRangeException();
            }

            CoreLoggers.Injection.Info("inject code with {0} at 0x{1}",
                this.GetType().FullName, addr.ToInt32().ToString("X8"));

            this.AddRegisterRead(Register.EAX);
            this.AddRegisterRead(Register.EBP);
            this.AddRegisterRead(Register.ECX);

            using (new ReadWriteProtect(addr, len))
            {
                IntPtr pCode;
                IntPtr pJumpForward = AssemblyCodeStorage.AllocateIndirect();
                IntPtr pJumpBackward = AssemblyCodeStorage.AllocateIndirect();

                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        WriteCode(bw);

                        byte[] moved = new byte[len];
                        Marshal.Copy(addr, moved, 0, len);
                        bw.Write(moved);

                        bw.Write((byte)0xFF);
                        bw.Write((byte)0x25);

                        //pointer to pointer
                        bw.Write(pJumpBackward.ToInt32());

                        var assemlyCode = ms.ToArray();
                        pCode = AssemblyCodeStorage.WriteCode(assemlyCode);
                    }
                }

                Marshal.WriteByte(addr, 0, 0xFF);
                Marshal.WriteByte(addr, 1, 0x25);
                var jmpForwardPtr = pJumpForward.ToInt32();
                Marshal.WriteInt32(addr, 2, jmpForwardPtr);

                //finally setup jump table
                AssemblyCodeStorage.WriteIndirect(pJumpForward, pCode);
                AssemblyCodeStorage.WriteIndirect(pJumpBackward, IntPtr.Add(addr, len));
            }
        }

        //TODO make a better api for other registers
        public IntPtr GetECX(NativeEnvironment env)
        {
            return env.GetRegister(Register.ECX);
        }
    }
}
