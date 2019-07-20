using AMLCore.Injection.Native;
using AMLCore.Misc;
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
        Standalone,
    }

    public static class Startup
    {
        internal static StartupMode Mode { get; private set; }

        public static void InitializeLauncher(string[] args)
        {
            if (args.Length == 1 && args[0] == OnlineUpdateCheck.RestartArg)
            {
                Mode = StartupMode.LauncherRestart;
                ThreadHelper.InitInternalThread("Main");
                StartupInternal.LauncherRestartStartup();
            }
            else if (args.Length == 1 && args[0] == StandaloneLauncher.StandaloneLauncherOption)
            {
                Mode = StartupMode.Standalone;
                ThreadHelper.InitInternalThread("Main");
                StartupInternal.StandaloneStartup(args[1]);
            }
            else
            {
                Mode = StartupMode.Launcher;
                ThreadHelper.InitInternalThread("Main");
                StartupInternal.LauncherStartup();
            }
        }

        public static void InitializeInjected(IntPtr ud)
        {
            Mode = StartupMode.Injected;
            ThreadHelper.InitInternalThread("Inject");
            StartupInternal.InjectedStartup(ud);
        }
    }

    internal static class StartupInternal
    {
        public static void LauncherStartup()
        {
            DebugPoint.Trigger();
        }

        public static void LauncherRestartStartup()
        {
            DebugPoint.Trigger();
            OnlineUpdateCheck.DoRestart();
        }

        public static void InjectedStartup(IntPtr ud)
        {
            DebugPoint.Trigger();
            CoreLoggers.Injection.Info("module handle: gs=0x{0}, gso=0x{1}",
                AddressHelper.Code(0).ToInt32().ToString("X8"),
                AddressHelper.Code("gso", 0).ToInt32().ToString("X8"));
            var args = InjectedArguments.Deserialize(ud);
            WindowsHelper.Init();
            PluginLoader.Load(args);
        }

        public static void StandaloneStartup(string name)
        {
            DebugPoint.Trigger();
            CoreLoggers.Loader.Info("standalone process starts");
            WindowsHelper.Init();
            //TODO
            WindowsHelper.MessageBox("Error: standalone is not supported.");
        }
    }
}
