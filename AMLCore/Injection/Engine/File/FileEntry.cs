using AMLCore.Injection.GSO;
using AMLCore.Injection.Native;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.File
{
    internal class FileEntry : IEntryPointLoad
    {
        public void Run()
        {
            new InjectCreateFileA(AddressHelper.Code(0x20E0B0));
            new InjectReadFile(AddressHelper.Code(0x20E09C));
            new InjectSetFilePointer(AddressHelper.Code(0x20E0A4));
            new InjectCloseHandle(AddressHelper.Code(0x20E0BC));

            if (PostGSOInjection.IsGSO)
            {
                PostGSOInjection.Run(() =>
                {
                    new InjectCreateFileW(AddressHelper.Code("gso", 0x1C088));
                    new InjectReadFile(AddressHelper.Code("gso", 0x1C0C8));
                    new InjectSetFilePointer(AddressHelper.Code("gso", 0x1C0E0));
                    new InjectCloseHandle(AddressHelper.Code("gso", 0x1C03C));
                });
            }
        }

        private delegate int CreateFileDelegate(IntPtr filename, int access, int share,
            IntPtr sec, int cd, int flags, int template);
        private class InjectCreateFileA : FunctionPointerInjection<CreateFileDelegate>
        {
            public InjectCreateFileA(IntPtr ptr)
                : base(ptr)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var p0 = env.GetParameterP(0);
                var p1 = env.GetParameterI(1);
                var p2 = env.GetParameterI(2);
                var p3 = env.GetParameterP(3);
                var p4 = env.GetParameterI(4);
                var p5 = env.GetParameterI(5);
                var p6 = env.GetParameterI(6);
                var ret = Original(p0, p1, p2, p3, p4, p5, p6);
                env.SetReturnValue(ret);

                FileReplacement.OpenFile(Marshal.PtrToStringAnsi(p0), p1, ret);
            }
        }
        private class InjectCreateFileW : FunctionPointerInjection<CreateFileDelegate>
        {
            public InjectCreateFileW(IntPtr ptr)
                : base(ptr)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var p0 = env.GetParameterP(0);
                var p1 = env.GetParameterI(1);
                var p2 = env.GetParameterI(2);
                var p3 = env.GetParameterP(3);
                var p4 = env.GetParameterI(4);
                var p5 = env.GetParameterI(5);
                var p6 = env.GetParameterI(6);
                var ret = Original(p0, p1, p2, p3, p4, p5, p6);
                env.SetReturnValue(ret);

                FileReplacement.OpenFile(Marshal.PtrToStringUni(p0), p1, ret);
            }
        }

        private delegate int ReadFileDelegate(int file, IntPtr buffer, int len, IntPtr read, IntPtr overlap);
        private class InjectReadFile : FunctionPointerInjection<ReadFileDelegate>
        {
            public InjectReadFile(IntPtr ptr)
                : base(ptr)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var p0 = env.GetParameterI(0);
                var p1 = env.GetParameterP(1);
                var p2 = env.GetParameterI(2);
                var p3 = env.GetParameterP(3);
                var p4 = env.GetParameterP(4);
                if (FileReplacement.ReadFile(p0, p1, p2, p3))
                {
                    env.SetReturnValue(1);
                }
                else
                {
                    env.SetReturnValue(Original(p0, p1, p2, p3, p4));
                }
            }
        }

        private delegate int SetFilePointerDelegate(int file, int dist, IntPtr dist2, int method);
        private class InjectSetFilePointer : FunctionPointerInjection<SetFilePointerDelegate>
        {
            public InjectSetFilePointer(IntPtr ptr)
                : base(ptr)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var p0 = env.GetParameterI(0);
                var p1 = env.GetParameterI(1);
                var p2 = env.GetParameterP(2);
                var p3 = env.GetParameterI(3);
                int ret;
                if (FileReplacement.SetFilePointer(p0, p1, p2, p3, out ret))
                {
                    env.SetReturnValue(ret);
                }
                else
                {
                    env.SetReturnValue(Original(p0, p1, p2, p3));
                }
            }
        }

        private delegate int CloseHandleDelegate(int handle);
        private class InjectCloseHandle : FunctionPointerInjection<CloseHandleDelegate>
        {
            public InjectCloseHandle(IntPtr ptr)
                : base(ptr)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var p0 = env.GetParameterI(0);
                FileReplacement.CloseHandle(p0);
                env.SetReturnValue(Original(p0));
            }
        }
    }
}
