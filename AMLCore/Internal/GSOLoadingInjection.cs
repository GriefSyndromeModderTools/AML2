using AMLCore.Injection.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Internal
{
    internal class GSOLoadingInjection
    {
        public static bool IsGSO => Marshal.ReadInt32(AddressHelper.Code(0x286080)) == 0x00730067;
        public static bool IsGSOLoaded => AddressHelper.Code("gso", 0) != IntPtr.Zero;

        public static bool RequireGSOLoading => IsGSO;

        private static InjectedArguments _arguments;

        public static void Inject()
        {
            new GSOReady();
        }

        public static void PreparePlugins(InjectedArguments args)
        {
            _arguments = args;
        }

        private static void InjectGSO()
        {

        }

        private static void StartGameServer()
        {

        }

        private static void StartGameClient()
        {

        }

        private class GSOReady : CodeInjection
        {
            public GSOReady() : base(0x1AB336, 7)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                InjectGSO();
            }
        }

        private class GSOStartGame
        {
            //TODO inject network and call StartGameServer/Client
            //need to send args through network when StartGame
        }
    }
}
