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
            try
            {
                SetupDependencyDllLocation();
                Program.Run(args);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private static void SetupDependencyDllLocation()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadCoreDll);
        }

        private static readonly Dictionary<string, Assembly> _LoadedAssemblies =
            new Dictionary<string, Assembly>();

        private static Assembly LoadCoreDll(object sender, ResolveEventArgs args)
        {
            lock (_LoadedAssemblies)
            {
                if (_LoadedAssemblies.TryGetValue(args.Name, out var ret))
                {
                    return ret;
                }

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
                    ret = Assembly.Load(data);
                    _LoadedAssemblies.Add(args.Name, ret);
                    return ret;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
