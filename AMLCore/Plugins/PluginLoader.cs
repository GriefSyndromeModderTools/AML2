﻿using AMLCore.Internal;
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

        public static PluginContainer GetTemporaryContainer(string name)
        {
            var path = PathHelper.GetPath("aml/mods/" + name + ".dll");
            if (!File.Exists(path)) return null;
            return new PluginContainer(Assembly.LoadFile(path), true);
        }

        private static PluginContainer InitAssembly(Assembly a, bool noEntry)
        {
            lock (_Plugins)
            {
                if (!_Plugins.TryGetValue(a, out var ret))
                {
                    ret = new PluginContainer(a, noEntry);
                    if (ret.InternalName == "RepRecorder")
                    {
                        CoreLoggers.Loader.Error("RepRecorder is replaced by an internal recorder in AML");
                        //Don't add to list.
                        return ret;
                    }
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
            while (loadList.Count > 0)
            {
                var p = loadList.Dequeue();
                try
                {
                    var c = InitAssembly(Assembly.LoadFile(p), false);
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
            RunEntryPoints();
            CoreLoggers.Loader.Info("finished");
        }

        public static PluginContainer[] LoadAllInLauncher()
        {
            var d = PathHelper.GetPath("aml/mods");
            var plugins = Directory.EnumerateFiles(d, "*.dll").ToArray();
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

        /// <summary>
        /// Check whether there are any effective/functional AML mods.
        /// Currently we enable anti-cheating when this function returns true.
        /// </summary>
        /// <returns></returns>
        public static bool ContainsFunctionalMods()
        {
            foreach (var p in _Plugins.Values)
            {
                if (p.Type != PluginType.Debug && p.Type != PluginType.Optimization)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
