using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Native
{
    public class CodeModification
    {
        private static readonly byte[] _NopArray;

        static CodeModification()
        {
            _NopArray = new byte[256];
            for (int i = 0; i < _NopArray.Length; ++i)
            {
                _NopArray[i] = 0x90;
            }
        }

        public static void Modify(uint offset, params byte[] code)
        {
            var addr = AddressHelper.Code(offset);
            using (new ReadWriteProtect(addr, code.Length))
            {
                OverlapCheck.Add(addr, code.Length);
                Marshal.Copy(code, 0, addr, code.Length);
            }
        }

        public static void Modify(string module, uint offset, params byte[] code)
        {
            var addr = AddressHelper.Code(module, offset);
            using (new ReadWriteProtect(addr, code.Length))
            {
                OverlapCheck.Add(addr, code.Length);
                Marshal.Copy(code, 0, addr, code.Length);
            }
        }

        public static void FillNop(uint offset, int len)
        {
            IntPtr addr = AddressHelper.Code(offset);
            using (new ReadWriteProtect(addr, len))
            {
                OverlapCheck.Add(addr, len);
                int i;
                for (i = 0; i + 256 <= len; i += 256, addr += 256)
                {
                    Marshal.Copy(_NopArray, 0, addr, 256);
                }
                Marshal.Copy(_NopArray, 0, addr, len - i);
            }
        }

        public static void WritePointer(IntPtr addr, IntPtr val)
        {
            using (new ReadWriteProtect(addr, 4))
            {
                Marshal.WriteIntPtr(addr, val);
            }
        }
    }
}
