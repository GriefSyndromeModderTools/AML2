using AMLCore.Injection.Native;
using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.GSO
{
    public static class PostGSOInjection
    {
        private readonly static List<Action> _actionList = new List<Action>();

        public static bool IsGSO => Marshal.ReadInt32(AddressHelper.Code(0x286080)) == 0x00730067;
        public static bool IsGSOLoaded => AddressHelper.Code("gso", 0) != IntPtr.Zero;

        public static void Run(Action action)
        {
            if (IsGSOLoaded || !IsGSO)
            {
                CoreLoggers.Injection.Info("post-gso injection action {0}.{1} directly executed because {2}",
                    action.Method.DeclaringType.ToString(), action.Method.Name,
                    IsGSOLoaded ? "gso.dll already loaded" : "not in griefsyndrome_online.exe");
                action();
            }
            else
            {
                CoreLoggers.Injection.Info("add post-gso injection action {0}", action.Method.ToString());
                _actionList.Add(action);
            }
        }

        internal static void Invoke()
        {
            CoreLoggers.Injection.Info("gso.dll loaded at 0x{0}", AddressHelper.Code("gso", 0).ToInt32().ToString("X8"));
            CoreLoggers.Injection.Info("post-gso injection starts");
            _actionList.ForEach(aa => aa());
            _actionList.Clear();
            CoreLoggers.Injection.Info("post-gso injection finishes");
        }
    }
}
