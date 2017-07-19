using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AMLCore.Internal
{
    internal static class ArgumentHelper
    {
        public static string[] GetModFileList(string list)
        {
            var mods = list.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var d = PathHelper.GetPath("aml/mods");
            List<string> dlls = new List<string>();
            foreach (var m in mods)
            {
                var f = Path.Combine(d, m + ".dll");
                if (File.Exists(f))
                {
                    dlls.Add(f);
                }
                else
                {
                    CoreLoggers.Loader.Error("cannot initialize mod {0}", m);
                }
            }
            return dlls.ToArray();
        }
    }
}
