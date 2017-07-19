using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AMLInjected
{
    public class Entry
    {
        [DllExport("LoadCore")]
        public static uint LoadCore(IntPtr ud)
        {
            SetupDependencyDllLocation();
            return Helper.LoadCore(ud);
        }

        private static void SetupDependencyDllLocation()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadCoreDll);
        }

        private static Assembly LoadCoreDll(object sender, ResolveEventArgs args)
        {
            string folderPath = Path.GetDirectoryName(typeof(Entry).Assembly.Location);
            string assemblyPathCore = Path.Combine(folderPath,
                new AssemblyName(args.Name).Name + ".dll");
            string assemblyPathMods = Path.Combine(folderPath, "../mods",
                new AssemblyName(args.Name).Name + ".dll");

            try
            {
                if (File.Exists(assemblyPathCore))
                {
                    return Assembly.LoadFrom(assemblyPathCore);
                }
                else if (File.Exists(assemblyPathMods))
                {
                    return Assembly.LoadFrom(assemblyPathMods);
                }
            }
            catch
            {
            }
            return null;
        }
    }
}
