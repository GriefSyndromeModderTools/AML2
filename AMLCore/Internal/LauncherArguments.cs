using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AMLCore.Internal
{
    public class LauncherArguments
    {
        public bool RequiresGui { get; private set; }
        public string ProcessName { get; private set; }
        public int WaitLength { get; private set; }
        public string DllName { get; private set; }
        public string ExportName { get; private set; }
        public bool WaitProcess { get; private set; }

        public string Mods { get; private set; }
        public List<Tuple<string, string>> Options { get; private set; }

        private LauncherArguments()
        {
            RequiresGui = true;
            ProcessName = "griefsyndrome.exe";
            WaitLength = 250;
            DllName = "AMLInjected.dll";
            ExportName = "LoadCore";
            WaitProcess = false;

            Mods = null;
            Options = new List<Tuple<string, string>>();
        }

        private void ParseInternal(string[] args)
        {
            foreach (var a in args)
            {
                ParseInternal(a);
            }
        }

        #region Parse

        private void ParseInternal(string arg)
        {
            if (arg.Contains('"') || arg.Contains('\\'))
            {
                CoreLoggers.Loader.Error("invalid launcher argument {0}", arg);
                return;
            }
            if (!arg.Contains('='))
            {
                ParseInternal(arg, null);
            }
            else
            {
                int index = arg.IndexOf('=');
                ParseInternal(arg.Substring(0, index).Trim(),
                    arg.Substring(index + 1).Trim());
            }
        }

        private void ParseInternal(string key, string value)
        {
            if (key == "Mods")
            {
                if (Mods != null)
                {
                    CoreLoggers.Loader.Error("duplicate Mods argument");
                }
                else
                {
                    Mods = value;
                }
            }
            else if (key == "NoGui")
            {
                if (value != null || !RequiresGui)
                {
                    CoreLoggers.Loader.Error("invalid NoGui argument");
                }
                else
                {
                    RequiresGui = false;
                }
            }
            else
            {
                Options.Add(new Tuple<string, string>(key, value));
            }
        }

        public static LauncherArguments Parse(string[] args)
        {
            var ret = new LauncherArguments();
            ret.ParseInternal(args);
            return ret;
        }

        #endregion

        private void SetPluginOptions(PluginContainer[] plugins)
        {
            Dictionary<string, PluginContainer> pluginDict = plugins
                .ToDictionary(p => p.AssemblyName);
            foreach (var o in Options)
            {
                var key = o.Item1;
                var seg = key.Split('.');
                if (seg.Length != 2)
                {
                    continue;
                }
                PluginContainer p;
                if (!pluginDict.TryGetValue(seg[0], out p) || !p.HasOptions)
                {
                    CoreLoggers.Loader.Error("unrecognized option {0}", key);
                    continue;
                }
                p.AddOption(seg[1], o.Item2);
            }
        }

        private void GetPluginOptions(PluginContainer[] plugins)
        {
            Mods = String.Join(",", plugins.Select(p => p.AssemblyName));
            List<Tuple<string, string>> options = new List<Tuple<string, string>>();
            string currentPlugin = "";
            Action<string, string> append = (string a, string b) =>
            {
                options.Add(new Tuple<string, string>(currentPlugin + "." + a, b));
            };
            foreach (var p in plugins)
            {
                if (p.HasOptions)
                {
                    currentPlugin = p.AssemblyName;
                    p.GetOptions(append);
                }
            }
            Options = options;
        }

        public byte[] WriteInjectedData()
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(0);
                bw.Write(Mods != null);
                if (Mods != null) bw.Write(Mods);
                bw.Write(Options.Count);
                foreach (var o in Options)
                {
                    bw.Write(o.Item1);
                    bw.Write(o.Item2);
                }
                ms.Position = 0;
                ms.Write(BitConverter.GetBytes(ms.Length - 4), 0, 4);
                return ms.ToArray();
            }
        }

        public void LogOptions()
        {
            CoreLoggers.Loader.Info("launcher options: Mods = {0}, Options = {{{1}}}",
                Mods ?? "<all>",
                String.Join(", ", Options.Select(o => $"{o.Item1} = {o.Item2}")));
        }

        public bool ShowConfigDialog(bool allowOnlineInjection)
        {
            if (!RequiresGui)
            {
                return true;
            }

            string[] plugins;
            if (Mods != null)
            {
                plugins = ArgumentHelper.GetModFileList(Mods);
            }
            else
            {
                var d = PathHelper.GetPath("aml/mods");
                plugins = Directory.EnumerateFiles(d, "*.dll").ToArray();
            }

            var loadedPlugins = PluginLoader.LoadInLauncher(plugins);
            SetPluginOptions(loadedPlugins);

            var dialog = new LauncherOptionForm(loadedPlugins);
            if (dialog.ShowDialog() == DialogResult.Cancel)
            {
                CoreLoggers.Loader.Info("exit from option form");
                return false;
            }

            CoreLoggers.Loader.Info("config finished from option form");
            GetPluginOptions(dialog.Options);

            return true;
        }
    }
}
