﻿using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace AMLCore.Plugins
{
    internal class PluginContainer
    {
        private Assembly _Assembly;
        private string _AssemblyName, _InternalName, _AssemblyVersion;
        private IPluginDescription _Desc;
        private IPluginOption _Option;
        private IEntryPointPreload[] _Pre;
        private IEntryPointLoad[] _Load;
        private IEntryPointPostload[] _Post;
        private IEntryPointGSO[] _GSO;
        private IPresetProvider[] _Presets;
        private PluginType _Type;

        public PluginContainer(Assembly assembly)
        {
            _Assembly = assembly;
            _AssemblyName = assembly.GetName().Name;
            _AssemblyVersion = assembly.GetName().Version.ToString();
            CoreLoggers.Loader.Info("initializing assembly {0}", _AssemblyName);

            var d = CreateInstances<IPluginDescription>(assembly);
            if (d.Length == 1)
            {
                _Desc = d[0];
            }
            else if (d.Length > 2)
            {
                CoreLoggers.Loader.Error("more than 1 description object in {0}", _AssemblyName);
            }
            var o = CreateInstances<IPluginOption>(assembly);
            if (o.Length == 1)
            {
                _Option = o[0];
            }
            else if (o.Length > 2)
            {
                CoreLoggers.Loader.Error("more than 1 option object in {0}", _AssemblyName);
            }
            _Presets = CreateInstances<IPresetProvider>(assembly);
            _Type = _Desc?.PluginType ?? PluginType.Debug;
            _InternalName = _Desc?.InternalName ?? "unknown";
            CoreLoggers.Loader.Info("initialized assembly {0}", _AssemblyName);
        }

        public void LoadNormalEntry()
        {
            if (_Pre != null) return;
            _Pre = CreateInstances<IEntryPointPreload>(_Assembly);
            _Load = CreateInstances<IEntryPointLoad>(_Assembly);
            _Post = CreateInstances<IEntryPointPostload>(_Assembly);
        }

        public void LoadGSOEntry()
        {
            if (_GSO != null) return;
            _GSO = CreateInstances<IEntryPointGSO>(_Assembly);
        }

        private static T[] CreateInstances<T>(Assembly a) where T : class
        {
            return CreateInstances<T>(FindTypes(a, typeof(T)));
        }

        private static IEnumerable<Type> FindTypes(Assembly a, Type b)
        {
            return a.GetTypes().Where(
                    t => t != null &&
                        t.IsClass &&
                        !t.IsAbstract &&
                        //b.IsAssignableFrom(t)
                        t.GetInterface(b.FullName) != null
                        );
        }

        private static T[] CreateInstances<T>(IEnumerable<Type> types) where T : class
        {
            List<T> ret = new List<T>();
            foreach (var t in types)
            {
                try
                {
                    var obj = Activator.CreateInstance(t);
                    if (obj is T cobj)
                    {
                        ret.Add(cobj);
                    }
                    else
                    {
                        var d1 = obj.GetType().GetInterfaces()[0].Assembly == typeof(T).Assembly;
                    }
                }
                catch (Exception e)
                {
                    CoreLoggers.Loader.Error("cannot load type {0}: {1}", t.FullName, e.ToString());
                }
            }
            return ret.ToArray();
        }

        public int Priority
        {
            get
            {
                return _Desc != null ? _Desc.LoadPriority : 0;
            }
        }

        public string AssemblyName
        {
            get { return _AssemblyName; }
        }

        public string InternalName
        {
            get { return _InternalName; }
        }

        public string AssemblyVersion
        {
            get { return _AssemblyVersion; }
        }

        public string DisplayName
        {
            get { return _Desc?.DisplayName ?? AssemblyName; }
        }

        public bool HasOptions
        {
            get { return _Option != null; }
        }

        public bool HasGSOLoaded
        {
            get { return _GSO != null && _GSO.Length > 0; }
        }

        public PluginType Type
        {
            get { return _Type; }
        }

        public string[] Dependencies
        {
            get { return _Desc?.Dependencies; }
        }

        public void GetOptions(Action<string, string> list)
        {
            if (_Option == null)
            {
                return;
            }
            try
            {
                _Option.GetOptions(list);
            }
            catch (Exception e)
            {
                CoreLoggers.Loader.Error("exception in getting options from {0}: {1}",
                    _AssemblyName, e.ToString());
            }
        }

        public void AddOption(string key, string value)
        {
            if (_Option == null)
            {
                return;
            }
            try
            {
                _Option.AddOption(key, value);
            }
            catch (Exception e)
            {
                CoreLoggers.Loader.Error("exception in adding option to {0}: {1}",
                    _AssemblyName, e.ToString());
            }
        }

        public void ResetOption()
        {
            try
            {
                _Option?.ResetOptions();
            }
            catch (Exception e)
            {
                CoreLoggers.Loader.Error("exception in resetting option to {0}: {1}",
                    _AssemblyName, e.ToString());
            }
        }

        public Control GetConfigControl()
        {
            if (_Option == null)
            {
                return null;
            }
            try
            {
                var x = _Option.GetConfigControl();
                if (x != null)
                {
                    return x;
                }
                var obj = _Option.GetPropertyWindowObject();
                if (obj != null)
                {
                    return new PropertyGrid() { SelectedObject = obj };
                }
            }
            catch (Exception e)
            {
                CoreLoggers.Loader.Error("exception in GetConfigControl for {0}: {1}",
                    _AssemblyName, e.ToString());
            }
            return null;
        }

        private void LogEntry(string phase, object obj)
        {
            CoreLoggers.Loader.Info("running {0} callback for {1}", phase, obj.GetType().FullName);
        }

        public void Preload()
        {
            var lastAssembly = _Assembly;
            PluginLoader.InitializingAssembly = _Assembly;
            Array.ForEach(_Pre, x => { LogEntry("preload", x); x.Run(); });
            PluginLoader.InitializingAssembly = _Assembly;
        }

        public void Load()
        {
            var lastAssembly = _Assembly;
            PluginLoader.InitializingAssembly = _Assembly;
            Array.ForEach(_Load, x => { LogEntry("load", x); x.Run(); });
            PluginLoader.InitializingAssembly = _Assembly;
        }

        public void Postload()
        {
            var lastAssembly = _Assembly;
            PluginLoader.InitializingAssembly = _Assembly;
            Array.ForEach(_Post, x => { LogEntry("postload", x); x.Run(); });
            PluginLoader.InitializingAssembly = _Assembly;
        }

        public void GSOLoad()
        {
            var lastAssembly = _Assembly;
            PluginLoader.InitializingAssembly = _Assembly;
            Array.ForEach(_GSO, x => { LogEntry("gso", x); x.Run(); });
            PluginLoader.InitializingAssembly = _Assembly;
        }

        public void CollectPresets(List<Preset> list)
        {
            foreach (var provider in _Presets)
            {
                try
                {
                    var l = provider.GetPresetList();
                    foreach (var pp in l)
                    {
                        var preset = new Preset(pp.Name, false);
                        preset.Mods = pp.PluginLists;
                        preset.Options.AddRange(pp.Options);
                        preset.SourcePlugin = AssemblyName;
                        list.Add(preset);
                    }
                }
                catch (Exception e)
                {
                    CoreLoggers.Loader.Error("exception in CollectPresets for {0}: {1}",
                        _AssemblyName, e.ToString());
                }
            }
        }

        public T GetExtension<T>() where T : class
        {
            return _Option as T;
        }
    }
}
