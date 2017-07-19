using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Native
{
    public class ReadWriteProtect : IDisposable
    {
        private IntPtr _Ptr;
        private int _Len;
        private Natives.Protection _OldProtect;

        public ReadWriteProtect(IntPtr ptr, int len)
        {
            if (ptr == IntPtr.Zero || len == 0)
            {
                return;
            }

            _Ptr = ptr;
            _Len = len;

            Natives.VirtualProtect(ptr, (uint)len,
                Natives.Protection.PAGE_EXECUTE_READWRITE, out _OldProtect);
        }

        public void Dispose()
        {
            if (_Ptr == IntPtr.Zero)
            {
                return;
            }
            Natives.VirtualProtect(_Ptr, (uint)_Len, _OldProtect, out _OldProtect);
            _Ptr = IntPtr.Zero;
        }
    }
}
