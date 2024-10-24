﻿using AMLCore.Injection.GSO;
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
        private enum ModCheckMode
        {
            Unknown = 0,
            Disabled = 1,
            Warn = 2,
            Synchronize = 3,
        }

        public static readonly bool IsGSO = Marshal.ReadInt32(AddressHelper.Code(0x286080)) == 0x00730067;
        public static bool IsGSOLoaded => AddressHelper.Code("gso", 0) != IntPtr.Zero;
        public static bool IsGameStarted { get; private set; }

        private static ModCheckMode _checkMode;
        private static ModCheckMode CheckMode => _checkMode == 0 ? throw new Exception("config not loaded") : _checkMode;
        public static bool ModCheckSync => CheckMode == ModCheckMode.Synchronize;
        public static bool ModCheck => CheckMode >= ModCheckMode.Warn;
        public static bool RequireGSOLoading => ModCheckSync;

        private static InjectedArguments _originalArguments;
        private static byte[] _replacedArguments;
        private static CommonArguments _mergedArguments;

        public static void LoadConfig()
        {
            var ini = new IniFile("Core");
            var syncType = ini.Read("GSO", "ModCheckMode", "check");
            if (syncType == "sync")
            {
                _checkMode = ModCheckMode.Synchronize;
            }
            else if (syncType == "check")
            {
                if (WindowsHelper.MessageBoxYesNo("联机Mod列表同步模式当前为“仅检查(check)”。\r\n" +
                    "此模式下客机只会接收主机汇报的Mod列表并检查是否一致，但不会主动改变已经选择的Mod，" +
                    "因此主机可能无法使用联机器上的Mod选择功能。\r\n\r\n" +
                    "是否将Mod列表同步模式改为“自动同步(sync)”？\r\n\r\n" +
                    "此提示只会出现一次。如果需要再次修改请自行编辑Core.ini，具体方法见使用说明。"))
                {
                    ini.Write("GSO", "ModCheckMode", "sync");
                    _checkMode = ModCheckMode.Synchronize;
                }
                else
                {
                    ini.Write("GSO", "ModCheckMode", "warn");
                    _checkMode = ModCheckMode.Warn;
                }
            }
            else if (syncType == "warn")
            {
                _checkMode = ModCheckMode.Warn;
            }
            else
            {
                _checkMode = ModCheckMode.Disabled;
            }
        }

        public static void Inject()
        {
            new GSOReady();
        }

        public static void PreparePlugins(InjectedArguments args)
        {
            _originalArguments = args;
            PluginLoader.Initialize(args, false, true);
            PluginLoader.RunGSOEntryPoints();
        }

        private static void OnGSOReady()
        {
            if (AddressHelper.Code("gso", 0) != IntPtr.Zero)
            {
                CoreLoggers.GSO.Info("gso.dll loaded at 0x{0}", AddressHelper.Code("gso", 0).ToInt32().ToString("X8"));
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
            GSOReplayRedirect.Inject();
            GSOWindowExtension.Inject();
            //TODO run gso entry point (maybe in PostGSOInjection)

            CoreLoggers.GSO.Info("post-gso injection finishes");
        }

        public static void ReplaceArguments(byte[] replacedArgs)
        {
            _replacedArguments = replacedArgs;
        }

        public static CommonArguments GetCurrentArguments()
        {
            if (_replacedArguments != null)
            {
                return InjectedArguments.Deserialize(_replacedArguments);
            }
            else
            {
                return _originalArguments;
            }
        }

        public static void ReplayGameStart()
        {
            GameStart("replay");
        }

        public static void ServerGameStart()
        {
            GameStart("server");
        }

        public static void ClientGameStart()
        {
            GameStart("client");
        }

        private static void GameStart(string sideName)
        {
            if (IsGameStarted) return;
            IsGameStarted = true;

            if (!RequireGSOLoading) return;

            if (_replacedArguments != null)
            {
                var a = InjectedArguments.Deserialize(_replacedArguments);
                _mergedArguments = new CommonArguments(new[] { _originalArguments });
                FunctionalModListHelper.ReplaceFunctionalMods(_mergedArguments, a);

                CoreLoggers.GSO.Info($"{sideName} starting with replaced argument: {_originalArguments}");
            }
            else
            {
                _mergedArguments = _originalArguments;
                CoreLoggers.GSO.Info($"{sideName} starting with original argument");
            }

            var th = new Thread(LoadingThreadEntry);
            th.Start();
            th.Join();
        }

        public static void ClientCheckArgs(byte[] replacedArgs, out bool argCheckResult, out bool versionCheckResult)
        {
            argCheckResult = versionCheckResult = false;
            var a = InjectedArguments.Deserialize(replacedArgs);
            if (FunctionalModListHelper.CompareFunctionalMods(GetCurrentArguments(), a))
            {
                argCheckResult = true;
                if (FunctionalModListHelper.CheckModVersion(a, out _))
                {
                    versionCheckResult = true;
                }
            }
        }

        public static bool ClientCheckModVersion(byte[] replacedArgs, out bool foundAll)
        {
            var a = InjectedArguments.Deserialize(replacedArgs);
            return FunctionalModListHelper.CheckModVersion(a, out foundAll);
        }

        public static byte[] ServerGetModString()
        {
            var a = FunctionalModListHelper.SelectFunctionalMods(GetCurrentArguments());
            FunctionalModListHelper.AddModVersionInfo(a);
            return a.Serialize(true, false);
        }

        private static void LoadingThreadEntry()
        {
            ThreadHelper.InitInternalThread("GSOInject");
            PluginLoader.Initialize(_mergedArguments, true, false);
            PluginLoader.RunEntryPoints();
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
