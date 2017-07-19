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
    public class Startup
    {
        private static bool _IsLauncher = Assembly.GetEntryAssembly() != null;

        internal static bool IsLauncher { get { return _IsLauncher; } }

        public static void Initialize()
        {
        }

        public static void Initialize(IntPtr ud)
        {
            if (!_IsLauncher)
            {
                CoreLoggers.Injection.Info("module handle: gs=0x{0}, gso=0x{1}",
                    AddressHelper.Code(0).ToInt32().ToString("X8"),
                    AddressHelper.Code("gso", 0).ToInt32().ToString("X8"));
            }
            var args = InjectedArguments.Deserialize(ud);
            PluginLoader.Load(args);
        }
    }
}
