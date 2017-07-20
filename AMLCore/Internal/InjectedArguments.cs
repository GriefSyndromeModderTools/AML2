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

            CoreLoggers.Loader.Info("injected options: Mods = {0}, Options = {{{1}}}",
                ret.Mods ?? "<all>",
                String.Join(", ", ret.Options.Select(o => $"{o.Item1} = {o.Item2}")));
            return ret;
        }
    }
}
