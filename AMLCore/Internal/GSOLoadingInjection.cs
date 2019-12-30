using AMLCore.Injection.GSO;
using AMLCore.Injection.Native;
using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AMLCore.Internal
{
    internal class GSOLoadingInjection
    {
        public static readonly bool IsGSO = Marshal.ReadInt32(AddressHelper.Code(0x286080)) == 0x00730067;
        public static bool IsGSOLoaded => AddressHelper.Code("gso", 0) != IntPtr.Zero;

        public static bool RequireGSOLoading => ModCheckSync;
        public static readonly bool ModCheckSync, ModCheck;

        public static bool IsGameStarted { get; private set; }

        static GSOLoadingInjection()
        {
            var syncType = new IniFile("Core").Read("GSO", "ModCheckMode", "sync");
            if (syncType == "sync")
            {
                ModCheck = ModCheckSync = true;
            }
            else if (syncType == "check")
            {
                ModCheck = true;
                ModCheckSync = false;
            }
            else
            {
                ModCheck = ModCheckSync = false;
            }
        }

        private static InjectedArguments _arguments;

        public static void Inject()
        {
            new GSOReady();
        }

        public static void PreparePlugins(InjectedArguments args)
        {
            _arguments = args;
        }

        private static void OnGSOReady()
        {
            if (AddressHelper.Code("gso", 0) != IntPtr.Zero)
            {
                CoreLoggers.GSO.Info("gso.dll loaded at 0x{0}", AddressHelper.Code("gso", 0).ToInt32().ToString("X8"));
            }
            else if (!IsGSO)
            {
                CoreLoggers.GSO.Info("not in griefsyndrome_online.exe");
            }
            else
            {
                CoreLoggers.GSO.Error("gso.dll not successfully loaded in griefsyndrome_online.exe");
            }
            CoreLoggers.GSO.Info("post-gso injection starts");

            //All gso related injection goes here.

            GSOConnectionMonitor.Inject();
            GSOWindowLog.Inject();
            PostGSOInjection.Invoke();
            //TODO run gso entry point (maybe in PostGSOInjection)

            CoreLoggers.GSO.Info("post-gso injection finishes");
        }

        //Also called when playing rep files.
        public static void ServerGameStart()
        {
            if (IsGameStarted) return;
            IsGameStarted = true;

            if (!RequireGSOLoading) return;
            CoreLoggers.GSO.Info("server starting with original argument");

            var th = new Thread(LoadingThreadEntry);
            th.Start();
            th.Join();
        }

        public static void ClientGameStart(byte[] replacedArgs)
        {
            if (IsGameStarted) return;
            IsGameStarted = true;

            if (!RequireGSOLoading) return;
            if (replacedArgs != null)
            {
                var a = InjectedArguments.Deserialize(replacedArgs);
                FunctionalModListHelper.ReplaceFunctionalMods(_arguments, a);

                CoreLoggers.GSO.Info("client starting with replaced argument: {0}", _arguments.ToString());
            }
            else
            {
                CoreLoggers.GSO.Info("client starting with original argument");
            }

            var th = new Thread(LoadingThreadEntry);
            th.Start();
            th.Join();
        }

        public static bool ClientCheckArgs(byte[] replacedArgs)
        {
            var a = InjectedArguments.Deserialize(replacedArgs);
            return FunctionalModListHelper.CompareFunctionalMods(_arguments, a);
        }

        public static byte[] ServerGetModString()
        {
            var a = FunctionalModListHelper.SelectFunctionalMods(_arguments);
            return a.Serialize();
        }

        private static void LoadingThreadEntry()
        {
            ThreadHelper.InitInternalThread("GSOInject");
            PluginLoader.Load(_arguments);
        }

        private class GSOReady : CodeInjection
        {
            public GSOReady() : base(0x1AB336, 7)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                OnGSOReady();
            }
        }
    }
}
