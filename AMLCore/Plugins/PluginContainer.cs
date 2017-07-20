using AMLCore.Internal;
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
        private string _AssemblyName;
        private IPluginDescription _Desc;
        private IPluginOption _Option;
        private IEntryPointPreload[] _Pre;
        private IEntryPointLoad[] _Load;
        private IEntryPointPostload[] _Post;

        public PluginContainer(Assembly assembly, bool noEntry)
        {
            _AssemblyName = assembly.GetName().Name;
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
            if (!noEntry)
            {
                _Pre = CreateInstances<IEntryPointPreload>(assembly);
                _Load = CreateInstances<IEntryPointLoad>(assembly);
                _Post = CreateInstances<IEntryPointPostload>(assembly);
            }
            CoreLoggers.Loader.Info("initialized assembly {0}", _AssemblyName);
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
                        b.IsAssignableFrom(t));
        }

        private static T[] CreateInstances<T>(IEnumerable<Type> types) where T : class
        {
            List<T> ret = new List<T>();
            foreach (var t in types)
            {
                try
                {
                    var obj = Activator.CreateInstance(t);
                    var cobj = obj as T;
                    if (cobj != null)
                    {
                        ret.Add(cobj);
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

        public string DisplayName
        {
            get { return _Desc?.DisplayName ?? AssemblyName; }
        }

        public bool HasOptions
        {
            get { return _Option != null; }
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
            Array.ForEach(_Pre, x => { LogEntry("preload", x); x.Run(); });
        }

        public void Load()
        {
            Array.ForEach(_Load, x => { LogEntry("load", x); x.Run(); });
        }

        public void Postload()
        {
            Array.ForEach(_Post, x => { LogEntry("postload", x); x.Run(); });
        }
    }
}
