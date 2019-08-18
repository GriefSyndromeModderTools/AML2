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

        private static PluginContainer InitAssembly(Assembly a, bool noEntry)
        {
            lock (_Plugins)
            {
                if (!_Plugins.TryGetValue(a, out var ret))
                {
                    ret = new PluginContainer(a, noEntry);
                    _Plugins.Add(a, ret);
                }
                return ret;
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
            Queue<string> loadList = new Queue<string>(args.GetPluginFiles());
            HashSet<string> allPlanned = new HashSet<string>(loadList);
            while (loadList.Count > 0)
            {
                var p = loadList.Dequeue();
                try
                {
                    var c = InitAssembly(Assembly.LoadFile(p), false);
                    foreach (var dep in c.Dependencies ?? new string[0])
                    {
                        if (allPlanned.Add(dep))
                        {
                            CoreLoggers.Loader.Info("load plugin {1} requested by {0}", p, dep);
                            loadList.Enqueue(dep);
                        }
                    }
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
