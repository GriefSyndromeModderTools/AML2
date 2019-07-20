using AMLCore.Internal;
using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AMLCore.Plugins
{
    internal class PluginLoader
    {
        private static Dictionary<Assembly, PluginContainer> _Plugins =
            new Dictionary<Assembly, PluginContainer>();

        private static void InitCorePlugin()
        {
            InitAssembly(typeof(PluginLoader).Assembly, false);
        }

        private static void InitAssembly(Assembly a, bool noEntry)
        {
            lock (_Plugins)
            {
                if (_Plugins.ContainsKey(a))
                {
                    return;
                }
                _Plugins[a] = new PluginContainer(a, noEntry);
            }
        }

        private static void RunEntryPoints()
        {
            var list = _Plugins.Values.OrderByDescending(p => p.Priority).ToArray();
            foreach (var p in list)
            {
                try
                {
                    p.Preload();
                }
                catch (Exception e)
                {
                    CoreLoggers.Loader.Error("exception in preload callback of {0}: {1}",
                        p.AssemblyName, e.ToString());
                }
            }
            foreach (var p in list)
            {
                try
                {
                    p.Load();
                }
                catch (Exception e)
                {
                    CoreLoggers.Loader.Error("exception in load callback of {0}: {1}",
                        p.AssemblyName, e.ToString());
                }
            }
            foreach (var p in list)
            {
                try
                {
                    p.Postload();
                }
                catch (Exception e)
                {
                    CoreLoggers.Loader.Error("exception in postload callback of {0}: {1}",
                        p.AssemblyName, e.ToString());
                }
            }
        }

        public static void Load(InjectedArguments args)
        {
            InitCorePlugin();
            foreach (var p in args.GetPluginFiles())
            {
                try
                {
                    InitAssembly(Assembly.LoadFile(p), false);
                }
                catch (Exception e)
                {
                    CoreLoggers.Loader.Error("exception in initialization of {0}: {1}",
                        p, e.ToString());
                }
            }
            args.SetPluginOptions(_Plugins.Values.ToArray());
            RunEntryPoints();
            CoreLoggers.Loader.Info("finished");
        }

        public static PluginContainer[] LoadInLauncher(string[] plugins)
        {
            foreach (var p in plugins)
            {
                try
                {
                    InitAssembly(Assembly.LoadFile(p), true);
                }
                catch (Exception e)
                {
                    CoreLoggers.Loader.Error("exception in initialization of {0}: {1}",
                        p, e.ToString());
                }
            }
            return _Plugins.Values.ToArray();
        }
    }
}
