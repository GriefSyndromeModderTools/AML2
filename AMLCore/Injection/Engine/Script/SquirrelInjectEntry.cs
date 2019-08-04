using AMLCore.Injection.Native;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.Script
{
    internal class SquirrelInjectEntry : IEntryPointLoad
    {
        //init
        public static IntPtr SquirrelVM { get; private set; }
        public static event Action<IntPtr> OnSquirrelCreated;
        private static object _Mutex = new object();
        private static bool _Created = false;

        //compile
        private struct CompileFileCall
        {
            public SquirrelFunctions.SQObject Table;
            public string FileName;
        }
        private static Stack<CompileFileCall> _CallStack = new Stack<CompileFileCall>();

        public void Run()
        {
            //init
            new InjectSquirrelVM();
            //compile
            new InjectBeforeArgs(0);
            new InjectBeforeArgs(1);
            new InjectBeforeRun(0);
            new InjectBeforeRun(1);
            new InjectAfterRun(0);
            new InjectAfterRun(1);
            new InjectCreateActCompileFile();
            //crash fix
            //World2D.SetXXXXFunction and CreateActor
            //old:
            //  cmp dword [eax], 0x08000100
            //  jne ...
            //new:
            //  test dword [eax], 0x00000300
            //  jz ...
            CodeModification.Modify(0x560F, 0xF7, 0x00, 0x00, 0x03, 0x00, 0x00, 0x74);
            CodeModification.Modify(0x2D9D, 0xF7, 0x00, 0x00, 0x03, 0x00, 0x00, 0x74);

            //similar fix related to CreateEventFromMap
            CodeModification.Modify(0x6304D, 0xF7, 0x00, 0x00, 0x03, 0x00, 0x00, 0x0F, 0x84);


            //add API functions
            SquirrelAPINewFunctions.Write_set();
            SquirrelAPINewFunctions.Write_rset();
            SquirrelAPINewFunctions.Write_rget();
        }

        private class InjectSquirrelVM : CodeInjection
        {
            public InjectSquirrelVM()
                : base(0xB69AA, 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var pVM = env.GetRegister(Register.EAX);
                SquirrelVM = pVM;

                SquirrelFunctions.pushroottable(pVM);
                SquirrelFunctions.pushstring(pVM, "MY_TEST_NUMBER", -1);
                SquirrelFunctions.pushinteger(pVM, 123);
                SquirrelFunctions.newslot(pVM, -3, 0);
                SquirrelFunctions.pop(pVM, 1);

                lock (_Mutex)
                {
                    if (!_Created)
                    {
                        _Created = true;
                        OnSquirrelCreated?.Invoke(pVM);
                    }
                }
            }
        }
        private class InjectBeforeArgs : CodeInjection
        {
            public InjectBeforeArgs(int index)
                : base((uint)(index == 0 ? 0xB71BF : 0xB72DC), 9)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var filename = Marshal.PtrToStringAnsi(env.GetParameterP(0));
                CompileFileInjectionManager.BeforeCompileFile(filename);
            }
        }

        private class InjectBeforeRun : CodeInjection
        {
            public InjectBeforeRun(int index)
                : base((uint)(index == 0 ? 0xB71E3 : 0xB7300), 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                CompileFileCall c = new CompileFileCall();
                if (SquirrelFunctions.getstackobj(SquirrelInjectEntry.SquirrelVM, -1, out c.Table) == 0)
                {
                    c.FileName = Marshal.PtrToStringAnsi(env.GetParameterP(0));
                    _CallStack.Push(c);
                }
            }
        }

        private class InjectAfterRun : CodeInjection
        {
            public InjectAfterRun(int index)
                : base((uint)(index == 0 ? 0xB720B : 0xB7328), 11)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var c = _CallStack.Pop();
                CompileFileInjectionManager.AfterCompileFile(c.FileName, ref c.Table);
            }
        }

        private class InjectCreateActCompileFile : CodeInjection
        {
            public InjectCreateActCompileFile()
                : base(0xE51AB, 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                CompileFileInjectionManager.CreateActBeforeRun();
            }
        }
    }
}
