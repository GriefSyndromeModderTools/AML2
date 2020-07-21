using AMLCore.Injection.Engine.Input;
using AMLCore.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace AMLCore.Misc
{
    public class AMLThreadAttribute : Attribute
    {
        public string Name { get; set; } = "<unknown>";
        public bool AutoExit { get; set; } = true;
    }

    public static class ThreadHelper
    {
        private class AMLThread
        {
            public AMLThreadAttribute Attribute;
            public string EntryName;
            public Thread Thread;
        }
        private static ConcurrentDictionary<Thread, AMLThread> _Threads =
            new ConcurrentDictionary<Thread, AMLThread>();
        private static ThreadLocal<string> _ThreadNameCache = new ThreadLocal<string>();

        public static Thread StartThread(ThreadStart entry)
        {
            var m = entry.Method;
            var attr = m.GetCustomAttributes(typeof(AMLThreadAttribute), true)
                .OfType<AMLThreadAttribute>().FirstOrDefault();
            if (attr == null) attr = new AMLThreadAttribute();
            var th = new Thread(entry);
            var entryName = m.DeclaringType.FullName + "." + m.Name;
            if (m.GetCustomAttributes(typeof(STAThreadAttribute), true).Count() != 0)
            {
                th.SetApartmentState(ApartmentState.STA);
            }
            var info = new AMLThread
            {
                Attribute = attr,
                Thread = th,
                EntryName = entryName,
            };
            _Threads.AddOrUpdate(th, k => info, (k1, k2) => info);
            CoreLoggers.Main.Info("new thread {0} on {1}", attr.Name, entryName);

            th.Start();
            return th;
        }

        private static string GetDefaultName()
        {
            return Thread.CurrentThread.ManagedThreadId.ToString();
        }

        private static string FindCurrentThreadName()
        {
            var th = Thread.CurrentThread;
            var info = _Threads.Values.FirstOrDefault(x => x.Thread == th);
            if (info == null)
            {
                return GetDefaultName();
            }
            return info.Attribute.Name;
        }

        public static string GetCurrentThreadName()
        {
            var ret = _ThreadNameCache.Value;
            if (ret == null)
            {
                ret = FindCurrentThreadName();
                _ThreadNameCache.Value = ret;
            }
            return ret;
        }

        internal static void TerminateAllThreads()
        {
            foreach (var th in _Threads.Values)
            {
                if (th.Thread.ThreadState == ThreadState.Running &&
                    th.Attribute.AutoExit)
                {
                    th.Thread.Abort();
                }
            }
            InputManager.MainWindowDestroyed = true;
        }

        internal static void InitInternalThread(string name)
        {
            if (_Threads.ContainsKey(Thread.CurrentThread))
            {
                return;
            }
            var info = new AMLThread
            {
                Attribute = new AMLThreadAttribute { AutoExit = false, Name = name },
                Thread = Thread.CurrentThread,
                EntryName = "<native>",
            };
            _Threads.AddOrUpdate(Thread.CurrentThread, k => info, (k1, k2) => info);
            _ThreadNameCache.Value = name;
        }
    }
}
