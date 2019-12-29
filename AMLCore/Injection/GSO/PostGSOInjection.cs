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

        public static bool IsGSO => GSOLoadingInjection.IsGSO;
        public static bool IsGSOLoaded => GSOLoadingInjection.IsGSOLoaded;

        public static void Run(Action action)
        {
            if (IsGSOLoaded || !IsGSO)
            {
                CoreLoggers.Injection.Info("post-gso injection action {0}.{1} directly executed because {2}",
                    action.Method.DeclaringType.FullName.ToString(), action.Method.Name,
                    IsGSOLoaded ? "gso.dll already loaded" : "not in griefsyndrome_online.exe");
                action();
            }
            else
            {
                CoreLoggers.Injection.Info("add post-gso injection action {0}.{1}",
                    action.Method.DeclaringType.FullName.ToString(), action.Method.Name);
                _actionList.Add(action);
            }
        }

        internal static void Invoke()
        {
            _actionList.ForEach(aa => aa());
            _actionList.Clear();
        }
    }
}
