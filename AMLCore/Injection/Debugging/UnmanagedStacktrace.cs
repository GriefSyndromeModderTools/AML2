using AMLCore.Injection.Native;
using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AMLCore.Injection.Debugging
{
    public class StacktraceResult
    {
        private readonly IntPtr _addr;

        public class Entry
        {
            public IntPtr[] List;
            public volatile int Count;
        }

        private readonly ThreadLocal<List<Entry>> _entries = new ThreadLocal<List<Entry>>(() => new List<Entry>());
        private readonly List<List<Entry>> _allEntryLists = new List<List<Entry>>();
        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        private static readonly ThreadLocal<IntPtr> _limit = new ThreadLocal<IntPtr>(() => GetStackLimit());

        public StacktraceResult(IntPtr addr)
        {
            _addr = addr;
        }

        public void Add(IntPtr ebp)
        {
            if (!_entries.IsValueCreated)
            {
                _lock.EnterWriteLock();
                try
                {
                    _allEntryLists.Add(_entries.Value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            _lock.EnterReadLock();
            try
            {
                var st = GetStacktrace(ebp);
                var list = _entries.Value;
                var find = list.FirstOrDefault(ii => Enumerable.SequenceEqual(ii.List, st));
                if (find == null)
                {
                    find = new Entry
                    {
                        List = st,
                        Count = 0,
                    };
                    list.Add(find);
                }
                Interlocked.Increment(ref find.Count);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public static IntPtr[] GetStacktrace(IntPtr ebp)
        {
            List<IntPtr> ret = new List<IntPtr>();
            do
            {
                ret.Add(Marshal.ReadIntPtr(ebp, 4));
            } while (TryGetStackPointer(ebp, out ebp));
            return ret.ToArray();
        }

        private static bool TryGetStackPointer(IntPtr val, out IntPtr next)
        {
            var nn = Marshal.ReadIntPtr(val);
            if (nn == IntPtr.Zero ||
                nn.ToInt32() < val.ToInt32() ||
                nn.ToInt32() > _limit.Value.ToInt32())
            {
                next = IntPtr.Zero;
                return false;
            }
            next = nn;
            return true;
        }

        private static IntPtr GetStackLimit()
        {
            Natives.GetCurrentThreadStackLimits(out _, out var ret);
            return ret;
        }

        public Entry[] GetAllResults()
        {
            List<Entry> collect = new List<Entry>();
            _lock.EnterWriteLock();
            try
            {
                foreach (var list in _allEntryLists)
                {
                    collect.AddRange(list);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            List<Entry> sort = new List<Entry>();
            foreach (var e in collect)
            {
                var find = sort.FirstOrDefault(ee => ee.List.SequenceEqual(e.List));
                if (find == null)
                {
                    find = new Entry
                    {
                        List = e.List,
                        Count = 0,
                    };
                    sort.Add(find);
                }
                find.Count += e.Count;
            }

            return sort.ToArray();
        }
    }

    public class UnmanagedStacktrace
    {
        public static StacktraceResult Inject(IntPtr addr, int size)
        {
            var ret = new StacktraceResult(addr);
            new Injection(addr, size, ret);
            return ret;
        }

        private class Injection : CodeInjection
        {
            private readonly StacktraceResult _result;

            public Injection(IntPtr addr, int len, StacktraceResult result) : base(addr, len)
            {
                _result = result;
            }

            protected override void Triggered(NativeEnvironment env)
            {
                _result.Add(env.GetRegister(Register.EBP));
            }
        }
    }
}
