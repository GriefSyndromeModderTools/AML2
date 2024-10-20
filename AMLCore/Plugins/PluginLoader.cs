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

        public static Assembly InitializingAssembly;

        private static void InitCorePlugin()
        {
            InitAssembly(typeof(PluginLoader).Assembly, true, true);
        }

        public static PluginContainer GetTemporaryContainer(string name)
        {
            var path = PathHelper.GetPath("aml/mods/" + name + ".dll");
            if (!File.Exists(path)) return null;
            return new PluginContainer(Assembly.LoadFile(path));
        }

        private static PluginContainer InitAssembly(Assembly a, bool loadNormalEntry, bool loadGSOEntry)
        {
            lock (_Plugins)
            {
                if (!_Plugins.TryGetValue(a, out var ret))
                {
                    ret = new PluginContainer(a);
                    if (ret.InternalName == "RepRecorder")
                    {
                        CoreLoggers.Loader.Error("RepRecorder has been replaced by an internal recorder in AML");
                        //Don't add to list.
                        return ret;
                    }
                    _Plugins.Add(a, ret);
                }
                if (loadNormalEntry)
                {
                    ret.LoadNormalEntry();
                }
                if (loadGSOEntry)
                {
                    ret.LoadGSOEntry();
                }
                return ret;
            }
        }

        public static void RunEntryPoints()
        {
            CoreLoggers.Loader.Info("run entry points");
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
            CoreLoggers.Loader.Info("entry points finished");
        }

        public static void RunGSOEntryPoints()
        {
            CoreLoggers.Loader.Info("run gso entry points");
            var list = _Plugins.Values.OrderByDescending(p => p.Priority).ToArray();
            foreach (var p in list)
            {
                try
                {
                    p.GSOLoad();
                }
                catch (Exception e)
                {
                    CoreLoggers.Loader.Error("exception in gsoload callback of {0}: {1}",
                        p.AssemblyName, e.ToString());
                }
            }
            CoreLoggers.Loader.Info("gso entry points finished");
        }

        public static void Initialize(InjectedArguments args, bool loadNormalEntry, bool loadGSOEntry)
        {
            InitCorePlugin();
            Queue<string> loadList = new Queue<string>(args.GetPluginFiles());
            while (loadList.Count > 0)
            {
                var p = loadList.Dequeue();
                try
                {
                    var c = InitAssembly(Assembly.LoadFile(p), loadNormalEntry, loadGSOEntry);
                    if (c.Dependencies != null && c.Dependencies.Length > 0)
                    {
                        CoreLoggers.Loader.Error("plugin dependency is no longer supported");
                    }
                }
                catch (Exception e)
                {
                    CoreLoggers.Loader.Error("exception in initialization of {0}: {1}",
                        p, e.ToString());
                }
            }
            args.SetPluginOptions(_Plugins.Values.ToArray());
            CoreLoggers.Loader.Info("initialization finished");
        }

        public static PluginContainer[] InitializeAllInGame()
        {
            //Same as launcher.
            return InitializeAllInLauncher();
        }

        public static PluginContainer[] InitializeAllInLauncher()
        {
            var d = PathHelper.GetPath("aml/mods");
            var plugins = Directory.EnumerateFiles(d, "*.dll").ToArray();
            foreach (var p in plugins)
            {
                try
                {
                    InitAssembly(Assembly.LoadFile(p), false, false);
                }
                catch (Exception e)
                {
                    CoreLoggers.Loader.Error("exception in initialization of {0}: {1}",
                        p, e.ToString());
                }
            }
            return _Plugins.Values.ToArray();
        }

        /// <summary>
        /// Check whether there are any effective/functional AML mods.
        /// Currently we enable anti-cheating when this function returns true.
        /// </summary>
        /// <returns></returns>
        public static bool ContainsFunctionalMods(bool considerACIgnoreList)
        {
            foreach (var p in _Plugins.Values)
            {
                if (considerACIgnoreList && AMLFeatureLevel.IsInACIgnoreList(p.InternalName))
                {
                    continue;
                }
                if (p.Type != PluginType.Debug && p.Type != PluginType.Optimization)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
