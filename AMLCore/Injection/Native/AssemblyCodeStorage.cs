using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Native
{
    public class AssemblyCodeStorage
    {
        public static IntPtr WriteCode(byte[] code)
        {
            var ret = AllocateCode(code.Length);
            WriteCode(ret, code);
            return ret;
        }

        public static IntPtr AllocateCode(int length)
        {
            return Natives.VirtualAlloc(IntPtr.Zero, (IntPtr)length,
                Natives.AllocationType.COMMIT, Natives.Protection.PAGE_EXECUTE_READWRITE);
        }

        public static void WriteCode(IntPtr addr, byte[] code)
        {
            Marshal.Copy(code, 0, addr, code.Length);
        }

        [Obsolete("use the delegate conversion api to avoid cast exception")]
        public static IntPtr WrapManagedDelegate(IntPtr d)
        {
            IntPtr newCode = AllocateCode(6);
            IntPtr jmp = AllocateIndirect();
            WriteIndirect(jmp, d);

            Marshal.WriteByte(newCode, 0, 0xFF);
            Marshal.WriteByte(newCode, 1, 0x25);
            Marshal.WriteInt32(newCode, 2, jmp.ToInt32());

            return newCode;
        }

        [Obsolete("use the delegate conversion api to avoid cast exception")]
        public static IntPtr WrapManagedDelegate(Delegate d)
        {
            return WrapManagedDelegate(Marshal.GetFunctionPointerForDelegate(d));
        }

        public static IntPtr AllocateIndirect()
        {
            if (_IndirectPointer.ToInt32() >= _IndirectEnd.ToInt32())
            {
                _IndirectPointer = Marshal.AllocHGlobal(128);
                _IndirectEnd = _IndirectPointer + 128;
            }
            var ret = _IndirectPointer;
            _IndirectPointer += 4;
            return ret;
        }

        public static void WriteIndirect(IntPtr addr, IntPtr val)
        {
            Marshal.WriteInt32(addr, val.ToInt32());
        }

        public T SafeGetDelegate<T>(IntPtr ptr) where T : class
        {
            var raw = Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
            if ((object)raw is T ret)
            {
                return ret;
            }
            return (T)(object)Delegate.CreateDelegate(typeof(T), raw.Target, raw.Method);
        }

        private static IntPtr _IndirectPointer, _IndirectEnd;
    }
}
