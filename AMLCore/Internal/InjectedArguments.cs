using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Internal
{
    internal class InjectedArguments : CommonArguments
    {
        public static InjectedArguments Deserialize(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return new InjectedArguments();
            }
            var len = Marshal.ReadInt32(ptr);
            var buffer = new byte[len];
            Marshal.Copy(ptr + 4, buffer, 0, len);

            var ret = new InjectedArguments();
            using (var br = new BinaryReader(new MemoryStream(buffer)))
            {
                ret.Read(br);
            }

            return ret;
        }

        public static InjectedArguments Deserialize(byte[] data)
        {
            if (data == null)
            {
                return new InjectedArguments();
            }
            var len = BitConverter.ToInt32(data, 0);
            var buffer = new byte[len];
            Array.Copy(data, 4, buffer, 0, len);

            var ret = new InjectedArguments();
            using (var br = new BinaryReader(new MemoryStream(buffer)))
            {
                ret.Read(br);
            }

            return ret;
        }

        public override string ToString()
        {
            return string.Format("Mods = {0}, Options = {{{1}}}",
                Mods ?? "<all>",
                string.Join(", ", Options.Select(o => $"{o.Item1} = {o.Item2}")));
        }
    }
}
