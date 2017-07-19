using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AMLCore.Misc
{
    public class PathHelper
    {
        private static string _GamePath;

        static PathHelper()
        {
            var loc = typeof(PathHelper).Assembly.Location;
            if (loc == null || loc.Length == 0)
            {
                //this is in launcher, where core is loaded from memory
                _GamePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
            else
            {
                var core = Path.GetDirectoryName(typeof(PathHelper).Assembly.Location);
                var aml = Path.GetDirectoryName(core);
                _GamePath = Path.GetDirectoryName(aml);
            }
        }

        public static string GetPath(string rel)
        {
            return Path.GetFullPath(Path.Combine(_GamePath, rel));
        }
    }
}
