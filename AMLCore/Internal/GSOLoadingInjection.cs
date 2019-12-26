﻿using AMLCore.Injection.GSO;
using AMLCore.Injection.Native;
using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Internal
{
    internal class GSOLoadingInjection
    {
        public static readonly bool IsGSO = Marshal.ReadInt32(AddressHelper.Code(0x286080)) == 0x00730067;
        public static bool IsGSOLoaded => AddressHelper.Code("gso", 0) != IntPtr.Zero;

        public static bool RequireGSOLoading => false; //ModCheckSync;
        public static readonly bool ModCheckSync, ModCheck;

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
            //All gso related injection goes here.

            PostGSOInjection.Invoke();
            GSOConnectionMonitor.Inject();
            GSOWindowLog.Inject();
            //TODO run gso entry point (maybe in PostGSOInjection)
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
