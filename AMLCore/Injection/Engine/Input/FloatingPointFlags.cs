using AMLCore.Injection.Native;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.Input
{
    internal class FloatingPointFlags
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GeneratedDelegate();

        private static GeneratedDelegate _Generated;
        private static object _Mutex = new object();

        //private delegate uint GetCtrlBitsDelegate();
        //private static GetCtrlBitsDelegate _getCtrlBits;

        public static void Reset()
        {
            lock (_Mutex)
            {
                if (_Generated == null)
                {
                    var raw = AssemblyCodeStorage.WriteCode(Generate());
                    GeneratedDelegate f = (GeneratedDelegate)Marshal.GetDelegateForFunctionPointer(raw, typeof(GeneratedDelegate));
                    _Generated = f;
                }
                //if (_getCtrlBits == null)
                //{
                //    var data = AssemblyCodeStorage.WriteCode(new byte[]
                //    {
                //        0x55, 0x89, 0xE5, 0x50, 0x0F, 0xAE, 0x1C, 0x24, 0x58, 0x81, 0x64, 0x24, 0xFC, 0xFF, 0x9F, 0x00, 0x00, 0x81, 0x4C, 0x24, 0xFC, 0x00, 0x40, 0x00, 0x00, 0x0F, 0xAE, 0x54, 0x24, 0xFC, 0x5D, 0xC3
                //    });
                //    _getCtrlBits = (GetCtrlBitsDelegate)Marshal.GetDelegateForFunctionPointer(data, typeof(GetCtrlBitsDelegate));
                //}
            }
            _Generated();
            //var old = _getCtrlBits();
        }

        private static byte[] Generate()
        {
            //where to ld and st
            int addr = Marshal.AllocHGlobal(4).ToInt32();
            int o1 = 0x4000, o2 = 0x0A00; //0800 or 0200?
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    //stmxcsr [?]
                    bw.Write((byte)0x0F);
                    bw.Write((byte)0xAE);
                    bw.Write((byte)0x1D);
                    bw.Write(addr);

                    //mov eax, ?
                    bw.Write((byte)0xB8);
                    bw.Write(o1);

                    //or [?], eax
                    bw.Write((byte)0x09);
                    bw.Write((byte)0x05);
                    bw.Write(addr);

                    //ldmxcsr [?]
                    bw.Write((byte)0x0F);
                    bw.Write((byte)0xAE);
                    bw.Write((byte)0x15);
                    bw.Write(addr);

                    //fstcw [?]
                    bw.Write((byte)0x9B);
                    bw.Write((byte)0xD9);
                    bw.Write((byte)0x3D);
                    bw.Write(addr);

                    //mov eax, ?
                    bw.Write((byte)0xB8);
                    bw.Write(o2);

                    //or [?], eax
                    bw.Write((byte)0x09);
                    bw.Write((byte)0x05);
                    bw.Write(addr);

                    //fldcw [?]
                    bw.Write((byte)0xD9);
                    bw.Write((byte)0x2D);
                    bw.Write(addr);

                    //xor eax, eax
                    bw.Write((byte)0x31);
                    bw.Write((byte)0xC0);

                    //ret
                    bw.Write((byte)0xC3);

                    return ms.ToArray();
                }
            }
        }
    }
}
