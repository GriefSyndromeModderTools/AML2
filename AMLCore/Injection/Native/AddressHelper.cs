using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Native
{
    public class AddressHelper
    {
        private static readonly IntPtr _ExeModule = Natives.GetModuleHandle(null);
        public static IntPtr Code(uint offset)
        {
            return _ExeModule + (int)offset;
        }

        public static IntPtr Code(string module, uint offset)
        {
            var m = Natives.GetModuleHandle(module);
            if (m == IntPtr.Zero)
            {
                return m;
            }
            return IntPtr.Add(m, (int)offset);
        }

        public static IntPtr VirtualTable(IntPtr obj, int index)
        {
            IntPtr pTable = Marshal.ReadIntPtr(obj);
            return IntPtr.Add(pTable, 4 * index);
        }
    }
}
