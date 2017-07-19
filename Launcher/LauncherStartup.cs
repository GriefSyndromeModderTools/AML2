using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Launcher
{
    class LauncherStartup
    {
        [STAThread]
        internal static void Main(string[] args)
        {
            SetupDependencyDllLocation();
            Program.Run(args);
        }

        private static void SetupDependencyDllLocation()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadCoreDll);
        }

        private static Assembly LoadCoreDll(object sender, ResolveEventArgs args)
        {
            string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyPath = Path.Combine(folderPath,
                "aml/core", new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath))
            {
                return null;
            }

            try
            {
                //Read the bytes instead of directly load from file.
                //This allows update functions to be implemented in the core dll.
                byte[] data = File.ReadAllBytes(assemblyPath);
                return Assembly.Load(data);
            }
            catch
            {
                return null;
            }
        }
    }
}
