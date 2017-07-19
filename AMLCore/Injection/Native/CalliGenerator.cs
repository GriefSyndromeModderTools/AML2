using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Native
{
    public class CalliGenerator
    {
        public static TDelegate Generate<TDelegate>(int offset)
        {
            //allowed parameter types:
            // this IntPtr: first parameter
            // struct type: direct mapping
            // out/ref struct type: convert to pointer and pin it on local
            // struct[]: pinned on local and convert to pointer
            // others: not supported
            //allowed return types:
            // struct type: direct mapping

            var destDelType = typeof(TDelegate);
            var method = destDelType.GetMethod("Invoke");

            //scan return type
            var retType = method.ReturnType;
            if (!retType.IsValueType || retType.IsByRef || retType.IsPointer)
            {
                throw new ArgumentException("invalid return type");
            }

            //scan parameters
            var parameters = method.GetParameters();
            var parameterCount = parameters.Length;
            var calliParameters = new Type[parameterCount];
            var pinnedList = new List<Type>();
            var pinnedParamIndex = new List<int>();
            var pinnedDest = new int[parameterCount];
            if (parameters[0].ParameterType != typeof(IntPtr))
            {
                throw new ArgumentException("first parameter must be IntPtr");
            }
            pinnedDest[0] = -1;
            calliParameters[0] = typeof(IntPtr);
            for (int i = 1; i < parameterCount; ++i)
            {
                var t = parameters[i].ParameterType;
                if (t.IsValueType && !t.IsByRef && !t.IsPointer)
                {
                    //direct mapping
                    pinnedDest[i] = -1;
                    calliParameters[i] = t;
                }
                else if ((t.IsByRef || t.IsArray) && t.GetElementType().IsValueType)
                {
                    //pin pointer
                    var ct = t.GetElementType().MakePointerType();
                    pinnedDest[i] = pinnedList.Count;
                    pinnedList.Add(ct);
                    pinnedParamIndex.Add(i);
                    calliParameters[i] = ct;
                }
                else
                {
                    throw new ArgumentException("invalid parameter type: " + t.ToString());
                }
            }

            //create method
            DynamicMethod calliMethod = new DynamicMethod("CalliInvoke", retType,
                    parameters.Select(p => p.ParameterType).ToArray(),
                    typeof(CalliGenerator).Module, true);
            ILGenerator generator = calliMethod.GetILGenerator();

            //declare locals (pinned)
            foreach (var p in pinnedList)
            {
                generator.DeclareLocal(p, true);
            }

            //store pinned arguments to pinned locals
            for (int i = 0; i < pinnedList.Count; ++i)
            {
                EmitLdarg(generator, pinnedParamIndex[i]);
                EmitStloc(generator, i);
            }

            //push args
            for (int i = 0; i < parameterCount; ++i)
            {
                if (pinnedDest[i] >= 0)
                {
                    EmitLdloc(generator, pinnedDest[i]);
                }
                else
                {
                    EmitLdarg(generator, i);
                }
            }

            //get the function pointer
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldind_I);
            generator.Emit(OpCodes.Ldc_I4, offset);
            generator.Emit(OpCodes.Conv_I);
            generator.Emit(OpCodes.Sizeof, typeof(void*));
            generator.Emit(OpCodes.Mul);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Ldind_I);

            //call and return
            generator.EmitCalli(OpCodes.Calli, CallingConvention.StdCall,
                retType, calliParameters);
            generator.Emit(OpCodes.Ret);

            return (TDelegate)(object)calliMethod.CreateDelegate(destDelType);
        }

        private static void EmitLdarg(ILGenerator generator, int i)
        {
            if (i == 0)
            {
                generator.Emit(OpCodes.Ldarg_0);
            }
            else if (i == 1)
            {
                generator.Emit(OpCodes.Ldarg_1);
            }
            else if (i == 2)
            {
                generator.Emit(OpCodes.Ldarg_2);
            }
            else if (i == 3)
            {
                generator.Emit(OpCodes.Ldarg_3);
            }
            else
            {
                generator.Emit(OpCodes.Ldarg, i);
            }
        }

        private static void EmitStloc(ILGenerator generator, int i)
        {
            if (i == 0)
            {
                generator.Emit(OpCodes.Stloc_0);
            }
            else if (i == 1)
            {
                generator.Emit(OpCodes.Stloc_1);
            }
            else if (i == 2)
            {
                generator.Emit(OpCodes.Stloc_2);
            }
            else if (i == 3)
            {
                generator.Emit(OpCodes.Stloc_3);
            }
            else
            {
                generator.Emit(OpCodes.Stloc, i);
            }
        }

        private static void EmitLdloc(ILGenerator generator, int i)
        {
            if (i == 0)
            {
                generator.Emit(OpCodes.Ldloc_0);
            }
            else if (i == 1)
            {
                generator.Emit(OpCodes.Ldloc_1);
            }
            else if (i == 2)
            {
                generator.Emit(OpCodes.Ldloc_2);
            }
            else if (i == 3)
            {
                generator.Emit(OpCodes.Ldloc_3);
            }
            else
            {
                generator.Emit(OpCodes.Ldloc, i);
            }
        }
    }
}
