using AMLCore.Injection.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Game.Resource
{
    class ResourceObject
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate void CloseAndFree(IntPtr pthis, bool free);
        //[1]: unknown
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate bool OpenPackage(IntPtr pthis, string packageFile);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate void ClosePackage(IntPtr pthis);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate bool Read(IntPtr pthis, IntPtr buffer, int len);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate int GetLastReadLength(IntPtr pthis);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate int Seek(IntPtr pthis, int offset, int origin);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate int GetLength(IntPtr pthis);

        private static IntPtr _vtab = IntPtr.Zero;
        private static List<object> _delegates = new List<object>();

        private static CloseAndFree GSCloseAndFree;
        //original layout:
        //[0]: vtab
        //[1]: file handle
        //[2]: last read len
        //[3]: length
        //[4]: raw stream offset
        //[5]: raw stream position
        //[6]: used by decryption

        //new layout:
        //[0]: vtab
        //[1]: 1/zero
        //[2]: last read len
        //[3]: length
        //[4]: internal instance id
        //[5]: current position
        //[6]: zero

        //functions:
        private static void CloseAndFreeImpl(IntPtr pthis, bool free)
        {
            Marshal.WriteInt32(pthis, 4, 0);
            if (free) GSCloseAndFree(pthis, true);
        }
        private static bool OpenPackageImpl(IntPtr pthis, string packageFile)
        {
            Marshal.WriteInt32(pthis, 4, 1);
            return true;
        }
        private static void ClosePackageImpl(IntPtr pthis)
        {
            Marshal.WriteInt32(pthis, 4, 0);
        }
        private static bool ReadImpl(IntPtr pthis, IntPtr buffer, int len)
        {
            var id = Marshal.ReadInt32(pthis, 4 * 4);
            var data = ResourceInjection.GetResource(id);

            var pos = Marshal.ReadInt32(pthis, 4 * 5);
            var totalLen = Marshal.ReadInt32(pthis, 4 * 3);
            if (pos + len > totalLen)
            {
                len = totalLen - pos;
            }
            Marshal.Copy(data, pos, buffer, len);
            Marshal.WriteInt32(pthis, 4 * 5, pos + len);
            Marshal.WriteInt32(pthis, 4 * 2, len);
            return true;
        }
        private static int GetLastReadLengthImpl(IntPtr pthis)
        {
            return Marshal.ReadInt32(pthis, 4 * 2);
        }
        private static int SeekImpl(IntPtr pthis, int offset, int origin)
        {
            var pos = Marshal.ReadInt32(pthis, 4 * 5);
            var totalLen = Marshal.ReadInt32(pthis, 4 * 3);
            switch (origin)
            {
                case 0: //begin
                    pos = offset;
                    break;
                case 1:
                    pos += offset;
                    break;
                case 2:
                    pos = totalLen + offset;
                    break;
            }
            if (pos < 0) pos = 0;
            if (pos > totalLen) pos = totalLen;
            Marshal.WriteInt32(pthis, 4 * 5, pos);
            return pos;
        }
        private static int GetLengthImpl(IntPtr pthis)
        {
            return Marshal.ReadInt32(pthis, 4 * 3);
        }

        static ResourceObject()
        {
            _vtab = Marshal.AllocHGlobal(4 * 7);

            CloseAndFree closeAndFree = CloseAndFreeImpl;
            _delegates.Add(closeAndFree);
            Marshal.WriteIntPtr(_vtab, 4 * 0, Marshal.GetFunctionPointerForDelegate(closeAndFree));
            OpenPackage openPackage = OpenPackageImpl;
            _delegates.Add(openPackage);
            Marshal.WriteIntPtr(_vtab, 4 * 1, Marshal.GetFunctionPointerForDelegate(openPackage));
            Marshal.WriteIntPtr(_vtab, 4 * 2, Marshal.GetFunctionPointerForDelegate(openPackage));
            ClosePackage closePackage = ClosePackageImpl;
            Marshal.WriteIntPtr(_vtab, 4 * 3, Marshal.GetFunctionPointerForDelegate(closePackage));
            Read read = ReadImpl;
            _delegates.Add(read);
            Marshal.WriteIntPtr(_vtab, 4 * 4, Marshal.GetFunctionPointerForDelegate(read));
            GetLastReadLength getLastReadLength = GetLastReadLengthImpl;
            _delegates.Add(getLastReadLength);
            Marshal.WriteIntPtr(_vtab, 4 * 5, Marshal.GetFunctionPointerForDelegate(getLastReadLength));
            Seek seek = SeekImpl;
            _delegates.Add(seek);
            Marshal.WriteIntPtr(_vtab, 4 * 6, Marshal.GetFunctionPointerForDelegate(seek));
            GetLength getLength = GetLengthImpl;
            _delegates.Add(getLength);
            Marshal.WriteIntPtr(_vtab, 4 * 7, Marshal.GetFunctionPointerForDelegate(getLength));

            GSCloseAndFree = (CloseAndFree)Marshal.GetDelegateForFunctionPointer(AddressHelper.Code(0x4AEC0),
                typeof(CloseAndFree));
        }

        public static void Init(IntPtr obj, int id, int len)
        {
            Marshal.WriteInt32(obj, 4 * 0, _vtab.ToInt32());
            Marshal.WriteInt32(obj, 4 * 1, 1);
            Marshal.WriteInt32(obj, 4 * 2, 0);
            Marshal.WriteInt32(obj, 4 * 3, len);
            Marshal.WriteInt32(obj, 4 * 4, id);
            Marshal.WriteInt32(obj, 4 * 5, 0);
            Marshal.WriteInt32(obj, 4 * 6, 0);
        }

        public static void CloseOriginal(IntPtr obj)
        {
            var pFunc = AddressHelper.VirtualTable(obj, 0);
            var func = Marshal.ReadIntPtr(pFunc);
            if (func == AddressHelper.Code(0x4AEC0))
            {
                GSCloseAndFree(obj, false);
            }
            else
            {
                var closeAndFree = (CloseAndFree)Marshal.GetDelegateForFunctionPointer(func,
                    typeof(CloseAndFree));
                closeAndFree(obj, false);
            }
        }
    }
}
