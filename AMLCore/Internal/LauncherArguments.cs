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
    public class LauncherArguments : CommonArguments
    {
        public bool RequiresGui { get; private set; } = true;
        public string ProcessName { get; private set; } = "griefsyndrome.exe";
        public int WaitLength { get; private set; } = 250;
        public string DllName { get; private set; } = "AMLInjected.dll";
        public string ExportName { get; private set; } = "LoadCore";
        public bool WaitProcess { get; private set; } = false;

        private LauncherArguments()
        {
        }

        #region Parse

        public static LauncherArguments Parse(string[] args)
        {
            var ret = new LauncherArguments();
            ret.ParseArgumentList(args);
            ret.RequiresGui = args.Length == 0;
            return ret;
        }

        protected override void ParseKeyValuePair(string key, string value)
        {
            if (key == "NoGui")
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
            else if (key == "WaitProcess")
            {
                if (value != null || WaitProcess)
                {
                    CoreLoggers.Loader.Error("invalid WaitProcess argument");
                }
                else
                {
                    WaitProcess = true;
                }
            }
            else
            {
                base.ParseKeyValuePair(key, value);
            }
        }

        #endregion
        
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
                    if (o.Item1 != null && o.Item2 != null)
                    {
                        bw.Write(o.Item1);
                        bw.Write(o.Item2);
                    }
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

            string[] plugins = GetPluginFiles();

            var loadedPlugins = PluginLoader.LoadInLauncher(plugins);
            SetPluginOptions(loadedPlugins);

            var dialog = new LauncherOptionForm(loadedPlugins, allowOnlineInjection);
            if (dialog.ShowDialog() == DialogResult.Cancel)
            {
                CoreLoggers.Loader.Info("exit from option form");
                return false;
            }

            CoreLoggers.Loader.Info("config finished from option form");
            GetPluginOptions(dialog.Options);
            switch (dialog.LauncherMode)
            {
                case LaunchMode.NewGame:
                    break;
                case LaunchMode.NewOnline:
                    ProcessName = "griefsyndrome_online.exe";
                    break;
            }
            return true;
        }
    }
}
