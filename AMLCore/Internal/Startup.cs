using AMLCore.Injection.Native;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AMLCore.Internal
{
    public enum StartupMode
    {
        Unknown,
        Launcher,
        LauncherRestart,
        Injected,
    }
    public class Startup
    {
        internal static StartupMode Mode { get; private set; }

        public static void Initialize(string[] args)
        {
            if (args.Length == 1 && args[0] == OnlineUpdateCheck.RestartArg)
            {
                Mode = StartupMode.LauncherRestart;
                OnlineUpdateCheck.DoRestart(args);
                return;
            }
            else
            {
                Mode = StartupMode.Launcher;
                if (args.Length == 0)
                {
                    OnlineUpdateCheck.Check();
                }
            }
        }

        public static void Initialize(IntPtr ud)
        {
            Mode = StartupMode.Injected;
            CoreLoggers.Injection.Info("module handle: gs=0x{0}, gso=0x{1}",
                AddressHelper.Code(0).ToInt32().ToString("X8"),
                AddressHelper.Code("gso", 0).ToInt32().ToString("X8"));
            var args = InjectedArguments.Deserialize(ud);
            PluginLoader.Load(args);
        }
    }
}
