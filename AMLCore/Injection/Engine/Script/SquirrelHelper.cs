using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.Script
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SquirrelFuncDelegate(IntPtr vm);

    public static class SquirrelHelper
    {
        public static IntPtr SquirrelVM => SquirrelInjectEntry.SquirrelVM;

        //prevent GC collecting
        private static List<SquirrelFuncDelegate> _DelegateRef = new List<SquirrelFuncDelegate>();

        public static SquirrelFuncDelegate Wrap(SquirrelFuncDelegate original)
        {
            return vm =>
            {
                try
                {
                    return original(vm);
                }
                catch (Exception e)
                {
                    CoreLoggers.Script.Error("C# wrapper exception at {0}.{1}: {2}.",
                        original.Method.DeclaringType.ToString(), original.Method.Name, e.ToString());
                    return -1;
                }
            };
        }

        public static void Run(Action<IntPtr> action)
        {
            var vm = SquirrelVM;
            if (vm == IntPtr.Zero)
            {
                SquirrelInjectEntry.OnSquirrelCreated += action;
            }
            else
            {
                action(vm);
            }
        }

        public static ReferencedScriptObject GetNewClosure(SquirrelFuncDelegate func)
        {
            func = Wrap(func);

            _DelegateRef.Add(func);
            IntPtr pFunc = Marshal.GetFunctionPointerForDelegate(func);

            var ret = new ReferencedScriptObject();

            Run(vm =>
            {
                SquirrelFunctions.newclosure(SquirrelVM, pFunc, 0);
                ret.PopFromStack();
            });

            return ret;
        }

        public static void RegisterGlobalFunction(string name, SquirrelFuncDelegate func)
        {
            func = Wrap(func);
            _DelegateRef.Add(func);
            IntPtr pFunc = Marshal.GetFunctionPointerForDelegate(func);

            Run(vm =>
            {
                SquirrelFunctions.pushroottable(vm);
                SquirrelFunctions.pushstring(vm, name, -1);
                SquirrelFunctions.newclosure(vm, pFunc, 0);
                SquirrelFunctions.newslot(vm, -3, 0);
                SquirrelFunctions.pop(vm, 1);
            });
        }

        public static InjectedScriptFunction InjectCompileFile(string script, string func)
        {
            return CompileFileInjectionManager.InjectCompileFile(script, func);
        }

        public static InjectedScriptFunction InjectCompileFileMain(string script)
        {
            return CompileFileInjectionManager.InjectCompileFileMain(script);
        }

        public static ReferencedScriptObject CompileScriptFunction(string code, string name)
        {
            var ret = new ReferencedScriptObject();

            Run(vm =>
            {
                if (SquirrelFunctions.compilebuffer(vm, code, name, 0) == 0)
                {
                    ret.PopFromStack();
                }
            });

            return ret;
        }

        public static ReferencedScriptObject CompileScriptChildFunction(string code, string name)
        {
            var ret = new ReferencedScriptObject();

            Run(vm =>
            {
                SquirrelFunctions.newtable(vm);
                if (SquirrelFunctions.compilebuffer(vm, code, name, 0) == 0)
                {
                    SquirrelFunctions.push(vm, -2); //table func table
                    SquirrelFunctions.call(vm, 1, 0, 0); //table func
                    SquirrelFunctions.pop(vm, 1); //table
                    SquirrelFunctions.pushstring(vm, name, -1); //table name
                    if (SquirrelFunctions.get(vm, -2) == 0) //table (func?)
                    {
                        ret.PopFromStack(); //table
                    } //table
                } //table
                SquirrelFunctions.pop(vm, 1);
            });

            return ret;
        }

        public static PopStackScopeGuard PushMemberChainThis(params ManagedSQObject[] names)
        {
            SquirrelFunctions.push(SquirrelVM, 1);
            return ReplaceMemberChainTop(names);
        }

        public static PopStackScopeGuard PushMemberChainRoot(params ManagedSQObject[] names)
        {
            SquirrelFunctions.pushroottable(SquirrelVM);
            return ReplaceMemberChainTop(names);
        }

        public static PopStackScopeGuard PushMemberChainStack(int id, params ManagedSQObject[] names)
        {
            SquirrelFunctions.push(SquirrelVM, id);
            return ReplaceMemberChainTop(names);
        }

        public static PopStackScopeGuard PushMemberChainObj(SQObject obj, params ManagedSQObject[] names)
        {
            SquirrelFunctions.pushobject(SquirrelVM, obj);
            return ReplaceMemberChainTop(names);
        }

        private static PopStackScopeGuard ReplaceMemberChainTop(params ManagedSQObject[] names)
        {
            foreach (var nn in names)
            {
                nn.Push(SquirrelVM);
                if (SquirrelFunctions.get(SquirrelVM, -2) != 0)
                {
                    SquirrelFunctions.pop(SquirrelVM, 1);
                    SquirrelFunctions.pushnull(SquirrelVM);
                    return new PopStackScopeGuard(false);
                }
                SquirrelFunctions.remove(SquirrelVM, -2);
            }
            return new PopStackScopeGuard(true);
        }

        public class PopStackScopeGuard : IDisposable
        {
            private bool _pop = false;
            public bool IsSuccess { get; }

            public PopStackScopeGuard(bool suc)
            {
                IsSuccess = suc;
            }

            public void Dispose()
            {
                if (!_pop)
                {
                    _pop = true;
                    SquirrelFunctions.pop(SquirrelVM, 1);
                }
            }

            private void Clear()
            {
                _pop = true;
            }

            public int? TryPopInt32()
            {
                if (SquirrelFunctions.getinteger(SquirrelVM, -1, out var ret) == 0)
                {
                    Dispose();
                    return ret;
                }
                return null;
            }

            public int PopInt32()
            {
                if (SquirrelFunctions.getinteger(SquirrelVM, -1, out var ret) == 0)
                {
                    Dispose();
                    return ret;
                }
                throw new Exception("squirrel pop type error");
            }

            public float? TryPopFloat()
            {
                if (SquirrelFunctions.getfloat(SquirrelVM, -1, out var ret) == 0)
                {
                    Dispose();
                    return ret;
                }
                return null;
            }

            public float PopFloat()
            {
                if (SquirrelFunctions.getfloat(SquirrelVM, -1, out var ret) == 0)
                {
                    Dispose();
                    return ret;
                }
                throw new Exception("squirrel pop type error");
            }

            public string PopString()
            {
                //need to consider null
                if (SquirrelFunctions.gettype(SquirrelVM, -1) == (int)SQObject.SQObjectType.OT_NULL)
                {
                    Dispose();
                    return null;
                }
                if (SquirrelFunctions.getstring(SquirrelVM, -1, out var ret) == 0)
                {
                    Dispose();
                    return ret;
                }
                throw new Exception("squirrel pop type error");
            }

            public ReferencedScriptObject PopRefObject()
            {
                var ret = new ReferencedScriptObject();
                ret.PopFromStack();
                Clear();
                return ret;
            }

            public SQObject PopObject()
            {
                SquirrelFunctions.getstackobj(SquirrelVM, -1, out var ret);
                SquirrelFunctions.pop(SquirrelVM, 1);
                Clear();
                return ret;
            }

            public bool PopBool()
            {
                if (SquirrelFunctions.getbool(SquirrelVM, -1, out var ret) == 0)
                {
                    Dispose();
                    return ret != 0;
                }
                throw new Exception("squirrel pop type error");
            }
        }

        public static void NewSlot(ManagedSQObject key, ManagedSQObject value)
        {
            if (key.Type == ManagedSQObject.ManagedSQObjectType.Null)
            {
                //Special case: sq_newslot won't pop for us.
                throw new NullReferenceException();
            }
            key.Push(SquirrelVM);
            value.Push(SquirrelVM);
            if (SquirrelFunctions.newslot(SquirrelVM, -3, 0) != 0)
            {
                throw new Exception("newslot error");
            }
        }

        public static void Set(ManagedSQObject key, ManagedSQObject value)
        {
            key.Push(SquirrelVM);
            value.Push(SquirrelVM);
            if (SquirrelFunctions.set(SquirrelVM, -3) != 0)
            {
                SquirrelFunctions.pop(SquirrelVM, 2);
                throw new Exception("newslot error");
            }
        }

        public static int GetInt32(ManagedSQObject key)
        {
            key.Push(SquirrelVM);
            if (SquirrelFunctions.get(SquirrelVM, -2) != 0)
            {
                throw new Exception("sq_get error");
            }
            if (SquirrelFunctions.getinteger(SquirrelVM, -1, out var ret) != 0)
            {
                throw new Exception("squirrel pop type error");
            }
            SquirrelFunctions.pop(SquirrelVM, 1);
            return ret;
        }

        public static float GetFloat(ManagedSQObject key)
        {
            key.Push(SquirrelVM);
            if (SquirrelFunctions.get(SquirrelVM, -2) != 0)
            {
                throw new Exception("sq_get error");
            }
            if (SquirrelFunctions.getfloat(SquirrelVM, -1, out var ret) != 0)
            {
                throw new Exception("squirrel pop type error");
            }
            SquirrelFunctions.pop(SquirrelVM, 1);
            return ret;
        }

        public static string GetString(ManagedSQObject key)
        {
            key.Push(SquirrelVM);
            if (SquirrelFunctions.get(SquirrelVM, -2) != 0)
            {
                throw new Exception("sq_get error");
            }
            if (SquirrelFunctions.getstring(SquirrelVM, -1, out var ret) != 0)
            {
                throw new Exception("squirrel pop type error");
            }
            SquirrelFunctions.pop(SquirrelVM, 1);
            return ret;
        }

        public static bool GetBool(ManagedSQObject key)
        {
            key.Push(SquirrelVM);
            if (SquirrelFunctions.get(SquirrelVM, -2) != 0)
            {
                throw new Exception("sq_get error");
            }
            if (SquirrelFunctions.getbool(SquirrelVM, -1, out var ret) != 0)
            {
                throw new Exception("squirrel pop type error");
            }
            SquirrelFunctions.pop(SquirrelVM, 1);
            return ret != 0;
        }

        public static PopStackScopeGuard CallPush(params ManagedSQObject[] args)
        {
            foreach (var a in args)
            {
                a.Push(SquirrelVM);
            }
            if (SquirrelFunctions.call(SquirrelVM, args.Length, 1, 0) != 0)
            {
                throw new Exception("sq_call error");
            }
            return new PopStackScopeGuard(true);
        }

        public static void CallEmpty(params ManagedSQObject[] args)
        {
            foreach (var a in args)
            {
                a.Push(SquirrelVM);
            }
            if (SquirrelFunctions.call(SquirrelVM, args.Length, 0, 0) != 0)
            {
                throw new Exception("sq_call error");
            }
        }
    }
}
