using AMLCore.Injection.Native;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.Script
{
    public delegate int IntMetaMethodHandler(SQObject obj, int value);
    public delegate float FloatMetaMethodHandler(SQObject obj, float value);

    public static class MetaMethodHelper
    {
        public static void RegisterIntGetter(string field, IntMetaMethodHandler handler)
        {
            if (!MetaMethodInjectEntry.IntHandlers.TryGetValue(field, out var list))
            {
                list = new MetaMethodInjectEntry.ReplacedInt();
                MetaMethodInjectEntry.IntHandlers.Add(field, list);
            }
            list.GetterList.Add(handler);
        }

        public static void RegisterIntSetter(string field, IntMetaMethodHandler handler)
        {
            if (!MetaMethodInjectEntry.IntHandlers.TryGetValue(field, out var list))
            {
                list = new MetaMethodInjectEntry.ReplacedInt();
                MetaMethodInjectEntry.IntHandlers.Add(field, list);
            }
            list.SetterList.Insert(0, handler);
        }

        public static void RegisterFloatGetter(string field, FloatMetaMethodHandler handler)
        {
            if (!MetaMethodInjectEntry.FloatHandlers.TryGetValue(field, out var list))
            {
                list = new MetaMethodInjectEntry.ReplacedFloat();
                MetaMethodInjectEntry.FloatHandlers.Add(field, list);
            }
            list.GetterList.Add(handler);
        }

        public static void RegisterFloatSetter(string field, FloatMetaMethodHandler handler)
        {
            if (!MetaMethodInjectEntry.FloatHandlers.TryGetValue(field, out var list))
            {
                list = new MetaMethodInjectEntry.ReplacedFloat();
                MetaMethodInjectEntry.FloatHandlers.Add(field, list);
            }
            list.SetterList.Insert(0, handler);
        }
    }

    internal class MetaMethodInjectEntry : IEntryPointLoad
    {
        public void Run()
        {
            new InjectNewClosure();
        }

        public static Dictionary<string, ReplacedInt> IntHandlers = new Dictionary<string, ReplacedInt>();
        public static Dictionary<string, ReplacedFloat> FloatHandlers = new Dictionary<string, ReplacedFloat>();

        private const int GetterPtrI = 0x65B10;
        private const int SetterPtrI = 0x603F0;

        private const int GetterPtrF = 0x6FE40;
        private const int SetterPtrF = 0x198D0;

        public class ReplacedInt
        {
            public readonly List<IntMetaMethodHandler> GetterList = new List<IntMetaMethodHandler>();
            public readonly List<IntMetaMethodHandler> SetterList = new List<IntMetaMethodHandler>();

            private readonly static SquirrelFuncDelegate OriginalGetter = 
                (SquirrelFuncDelegate)Marshal.GetDelegateForFunctionPointer(AddressHelper.Code(GetterPtrI),
                    typeof(SquirrelFuncDelegate));
            private readonly static SquirrelFuncDelegate OriginalSetter =
                (SquirrelFuncDelegate)Marshal.GetDelegateForFunctionPointer(AddressHelper.Code(SetterPtrI),
                    typeof(SquirrelFuncDelegate));

            public readonly SquirrelFuncDelegate ReplacedGetter, ReplacedSetter;

            public ReplacedInt()
            {
                ReplacedGetter = SquirrelHelper.Wrap(GetterEntry);
                ReplacedSetter = SquirrelHelper.Wrap(SetterEntry);
            }

            private int GetterEntry(IntPtr vm)
            {
                var ret = OriginalGetter(vm);
                if (ret != 1) return ret;

                SquirrelFunctions.getinteger(vm, -1, out var val);
                SquirrelFunctions.pop(vm, 1);
                SquirrelFunctions.getstackobj(vm, 1, out var obj);
                foreach (var replace in GetterList)
                {
                    val = replace(obj, val);
                }
                SquirrelFunctions.pushinteger(vm, val);
                return 1;
            }

            private int SetterEntry(IntPtr vm)
            {
                SquirrelFunctions.getinteger(vm, 2, out var val);
                SquirrelFunctions.getstackobj(vm, 1, out var obj);
                foreach (var replace in SetterList)
                {
                    val = replace(obj, val);
                }

                var topPos = SquirrelFunctions.gettop(vm);

                SquirrelFunctions.pushinteger(vm, val);
                for (int i = 3; i <= topPos; ++i)
                {
                    SquirrelFunctions.push(vm, i);
                }
                for (int i = 2; i <= topPos; ++i)
                {
                    SquirrelFunctions.remove(vm, 2);
                }

                return OriginalSetter(vm);
            }
        }

        public class ReplacedFloat
        {
            public readonly List<FloatMetaMethodHandler> GetterList = new List<FloatMetaMethodHandler>();
            public readonly List<FloatMetaMethodHandler> SetterList = new List<FloatMetaMethodHandler>();

            private readonly static SquirrelFuncDelegate OriginalGetter =
                (SquirrelFuncDelegate)Marshal.GetDelegateForFunctionPointer(AddressHelper.Code(GetterPtrF),
                    typeof(SquirrelFuncDelegate));
            private readonly static SquirrelFuncDelegate OriginalSetter =
                (SquirrelFuncDelegate)Marshal.GetDelegateForFunctionPointer(AddressHelper.Code(SetterPtrF),
                    typeof(SquirrelFuncDelegate));

            public readonly SquirrelFuncDelegate ReplacedGetter, ReplacedSetter;

            public ReplacedFloat()
            {
                ReplacedGetter = SquirrelHelper.Wrap(GetterEntry);
                ReplacedSetter = SquirrelHelper.Wrap(SetterEntry);
            }

            private int GetterEntry(IntPtr vm)
            {
                var ret = OriginalGetter(vm);
                if (ret != 1) return ret;

                SquirrelFunctions.getfloat(vm, -1, out var val);
                SquirrelFunctions.pop(vm, 1);
                SquirrelFunctions.getstackobj(vm, 1, out var obj);
                foreach (var replace in GetterList)
                {
                    val = replace(obj, val);
                }
                SquirrelFunctions.pushfloat(vm, val);
                return 1;
            }

            private int SetterEntry(IntPtr vm)
            {
                SquirrelFunctions.getfloat(vm, 2, out var val);
                SquirrelFunctions.getstackobj(vm, 1, out var obj);
                foreach (var replace in SetterList)
                {
                    val = replace(obj, val);
                }

                var topPos = SquirrelFunctions.gettop(vm);

                SquirrelFunctions.pushfloat(vm, val);
                for (int i = 3; i <= topPos; ++i)
                {
                    SquirrelFunctions.push(vm, i);
                }
                for (int i = 2; i <= topPos; ++i)
                {
                    SquirrelFunctions.remove(vm, 2);
                }

                return OriginalSetter(vm);
            }
        }

        private class InjectNewClosure : CodeInjection
        {
            public InjectNewClosure() : base(0x12EA16, 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var funcPtr = env.GetParameterP(1);
                if (funcPtr == AddressHelper.Code(GetterPtrI))
                {
                    if (IntHandlers.TryGetValue(GetName(), out var handler))
                    {
                        env.SetParameter(1, Marshal.GetFunctionPointerForDelegate(handler.ReplacedGetter));
                    }
                }
                else if (funcPtr == AddressHelper.Code(SetterPtrI))
                {
                    if (IntHandlers.TryGetValue(GetName(), out var handler))
                    {
                        env.SetParameter(1, Marshal.GetFunctionPointerForDelegate(handler.ReplacedSetter));
                    }
                }
                else if (funcPtr == AddressHelper.Code(GetterPtrF))
                {
                    if (FloatHandlers.TryGetValue(GetName(), out var handler))
                    {
                        env.SetParameter(1, Marshal.GetFunctionPointerForDelegate(handler.ReplacedGetter));
                    }
                }
                else if (funcPtr == AddressHelper.Code(SetterPtrF))
                {
                    if (FloatHandlers.TryGetValue(GetName(), out var handler))
                    {
                        env.SetParameter(1, Marshal.GetFunctionPointerForDelegate(handler.ReplacedSetter));
                    }
                }
            }

            private static string GetName()
            {
                SquirrelFunctions.getstring(SquirrelHelper.SquirrelVM, -2, out var ret);
                return ret;
            }
        }
    }
}
