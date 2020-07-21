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
        public bool RequiresGui => !ArgsRequiresGui.HasValue || ArgsRequiresGui.Value;
        public bool? ArgsRequiresGui { get; private set; } = null;
        public string ProcessName { get; private set; } = "griefsyndrome.exe";
        public int WaitLength { get; private set; } = 250;
        public string DllName { get; private set; } = "AMLInjected.dll";
        public string ExportName { get; private set; } = "LoadCore";
        public bool WaitProcess { get; private set; } = false;
        public string PresetOptions { get; private set; } = null;

        private LauncherArguments()
        {
        }

        #region Parse

        public static LauncherArguments Parse(string[] args)
        {
            var ret = new LauncherArguments();
            ret.ParseArgumentList(args);
            if (!ret.ArgsRequiresGui.HasValue)
            {
                ret.ArgsRequiresGui = args.Length == 0;
            }
            return ret;
        }

        protected override void ParseKeyValuePair(string key, string value)
        {
            if (key == "NoGui")
            {
                if (value != null || ArgsRequiresGui.HasValue)
                {
                    CoreLoggers.Loader.Error("invalid NoGui argument");
                }
                else
                {
                    ArgsRequiresGui = false;
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
            else if (key == "ProcessName")
            {
                ProcessName = value;
            }
            else if (key == "Gui")
            {
                if (value != null || ArgsRequiresGui.HasValue)
                {
                    CoreLoggers.Loader.Error("invalid Gui argument");
                }
                else
                {
                    ArgsRequiresGui = true;
                }
            }
            else if (key == "PresetOptions")
            {
                if (value == null || PresetOptions != null)
                {
                    CoreLoggers.Loader.Error("invalid PresetOptions argument");
                }
                else
                {
                    PresetOptions = value;
                }
            }
            else
            {
                base.ParseKeyValuePair(key, value);
            }
        }

        #endregion

        //For compatibility.
        public byte[] WriteInjectedData()
        {
            return this.Serialize();
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

            var loadedPlugins = PluginLoader.InitializeAllInLauncher();
            SetPluginOptions(loadedPlugins);

            var dialog = new LauncherOptionForm(loadedPlugins, allowOnlineInjection, PresetOptions);
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
