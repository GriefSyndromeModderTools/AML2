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
        public enum SQObjectCategory
        {
            SQOBJECT_REF_COUNTED = 0x08000000,
            SQOBJECT_NUMERIC = 0x04000000,
            SQOBJECT_DELEGABLE = 0x02000000,
            SQOBJECT_CANBEFALSE = 0x01000000,
        }

        public enum RawType
        {
            _RT_NULL = 0x00000001,
            _RT_INTEGER = 0x00000002,
            _RT_FLOAT = 0x00000004,
            _RT_BOOL = 0x00000008,
            _RT_STRING = 0x00000010,
            _RT_TABLE = 0x00000020,
            _RT_ARRAY = 0x00000040,
            _RT_USERDATA = 0x00000080,
            _RT_CLOSURE = 0x00000100,
            _RT_NATIVECLOSURE = 0x00000200,
            _RT_GENERATOR = 0x00000400,
            _RT_USERPOINTER = 0x00000800,
            _RT_THREAD = 0x00001000,
            _RT_FUNCPROTO = 0x00002000,
            _RT_CLASS = 0x00004000,
            _RT_INSTANCE = 0x00008000,
            _RT_WEAKREF = 0x00010000,
        }

        public enum SQObjectType
        {
            OT_NULL = (RawType._RT_NULL | SQObjectCategory.SQOBJECT_CANBEFALSE),
            OT_INTEGER = (RawType._RT_INTEGER | SQObjectCategory.SQOBJECT_NUMERIC | SQObjectCategory.SQOBJECT_CANBEFALSE),
            OT_FLOAT = (RawType._RT_FLOAT | SQObjectCategory.SQOBJECT_NUMERIC | SQObjectCategory.SQOBJECT_CANBEFALSE),
            OT_BOOL = (RawType._RT_BOOL | SQObjectCategory.SQOBJECT_CANBEFALSE),
            OT_STRING = (RawType._RT_STRING | SQObjectCategory.SQOBJECT_REF_COUNTED),
            OT_TABLE = (RawType._RT_TABLE | SQObjectCategory.SQOBJECT_REF_COUNTED | SQObjectCategory.SQOBJECT_DELEGABLE),
            OT_ARRAY = (RawType._RT_ARRAY | SQObjectCategory.SQOBJECT_REF_COUNTED),
            OT_USERDATA = (RawType._RT_USERDATA | SQObjectCategory.SQOBJECT_REF_COUNTED | SQObjectCategory.SQOBJECT_DELEGABLE),
            OT_CLOSURE = (RawType._RT_CLOSURE | SQObjectCategory.SQOBJECT_REF_COUNTED),
            OT_NATIVECLOSURE = (RawType._RT_NATIVECLOSURE | SQObjectCategory.SQOBJECT_REF_COUNTED),
            OT_GENERATOR = (RawType._RT_GENERATOR | SQObjectCategory.SQOBJECT_REF_COUNTED),
            OT_USERPOINTER = RawType._RT_USERPOINTER,
            OT_THREAD = (RawType._RT_THREAD | SQObjectCategory.SQOBJECT_REF_COUNTED),
            OT_FUNCPROTO = (RawType._RT_FUNCPROTO | SQObjectCategory.SQOBJECT_REF_COUNTED), //internal usage only
            OT_CLASS = (RawType._RT_CLASS | SQObjectCategory.SQOBJECT_REF_COUNTED),
            OT_INSTANCE = (RawType._RT_INSTANCE | SQObjectCategory.SQOBJECT_REF_COUNTED | SQObjectCategory.SQOBJECT_DELEGABLE),
            OT_WEAKREF = (RawType._RT_WEAKREF | SQObjectCategory.SQOBJECT_REF_COUNTED)
        }

        public static IntPtr SquirrelVM => SquirrelInjectEntry.SquirrelVM;

        //prevent GC collecting
        private static List<SquirrelFuncDelegate> _DelegateRef = new List<SquirrelFuncDelegate>();

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
            Run(vm =>
            {
                _DelegateRef.Add(func);
                IntPtr pFunc = Marshal.GetFunctionPointerForDelegate(func);
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

        public static void GetMemberChainThis(params string[] names)
        {
            SquirrelFunctions.push(SquirrelVM, 1);
            GetMemberChainTop(names);
        }

        public static void GetMemberChainRoot(params string[] names)
        {
            SquirrelFunctions.pushroottable(SquirrelVM);
            GetMemberChainTop(names);
        }

        public static void GetMemberChainStack(int id, params string[] names)
        {
            SquirrelFunctions.push(SquirrelVM, id);
            GetMemberChainTop(names);
        }

        public static void GetMemberChainTop(params string[] names)
        {
            foreach (var nn in names)
            {
                SquirrelFunctions.pushstring(SquirrelVM, nn, -1);
                if (SquirrelFunctions.get(SquirrelVM, -2) != 0)
                {
                    CoreLoggers.Script.Error("sq_get error for {0} with name {1}", SquirrelFunctions.gettype(SquirrelVM, -1), nn);
                    SquirrelFunctions.pop(SquirrelVM, 1);
                    SquirrelFunctions.pushnull(SquirrelVM);
                    return;
                }
                SquirrelFunctions.remove(SquirrelVM, -2);
            }
        }
    }
}
