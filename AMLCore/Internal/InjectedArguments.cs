using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Internal
{
    public class InjectedArguments
    {
        public string Mods { get; private set; }
        public List<Tuple<string, string>> Options { get; private set; }

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
                if (br.ReadBoolean())
                {
                    ret.Mods = br.ReadString();
                }
                int c = br.ReadInt32();
                ret.Options = new List<Tuple<string, string>>();
                for (int i = 0; i < c; ++i)
                {
                    var key = br.ReadString();
                    var val = br.ReadString();
                    ret.Options.Add(new Tuple<string, string>(key, val));
                }
            }

            CoreLoggers.Loader.Info("injected options: Mods = {0}, Options = {{{1}}}",
                ret.Mods ?? "<all>",
                String.Join(", ", ret.Options.Select(o => $"{o.Item1} = {o.Item2}")));
            return ret;
        }

        public string[] GetPluginFiles()
        {
            if (Mods == null)
            {
                var d = PathHelper.GetPath("aml/mods");
                return Directory.EnumerateFiles(d, "*.dll").ToArray();
            }
            else
            {
                return ArgumentHelper.GetModFileList(Mods);
            }
        }
    }
}
