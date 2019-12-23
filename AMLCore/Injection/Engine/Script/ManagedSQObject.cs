using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Engine.Script
{
    public struct ManagedSQObject
    {
        public enum ManagedSQObjectType
        {
            Null,
            Bool,
            Int,
            Float,
            String,
            Root,
            RefObj,
            Parameter,
        }

        public ManagedSQObjectType Type;
        public int IntValue;
        public float FloatValue;
        public string StringValue;
        public SQObject RefObjValue;

        public static readonly ManagedSQObject Null = new ManagedSQObject
        {
            Type = ManagedSQObjectType.Null,
        };
        public static readonly ManagedSQObject Root = new ManagedSQObject
        {
            Type = ManagedSQObjectType.Root,
        };

        public static ManagedSQObject Parameter(int index)
        {
            return new ManagedSQObject
            {
                Type = ManagedSQObjectType.Parameter,
                IntValue = index,
            };
        }

        public static implicit operator ManagedSQObject(bool b)
        {
            return new ManagedSQObject
            {
                Type = ManagedSQObjectType.Bool,
                IntValue = b ? 1 : 0,
            };
        }

        public static implicit operator ManagedSQObject(int i)
        {
            return new ManagedSQObject
            {
                Type = ManagedSQObjectType.Int,
                IntValue = i,
            };
        }

        public static implicit operator ManagedSQObject(float f)
        {
            return new ManagedSQObject
            {
                Type = ManagedSQObjectType.Float,
                FloatValue = f,
            };
        }

        public static implicit operator ManagedSQObject(string s)
        {
            return new ManagedSQObject
            {
                Type = ManagedSQObjectType.String,
                StringValue = s,
            };
        }

        public static implicit operator ManagedSQObject(SQObject obj)
        {
            return new ManagedSQObject
            {
                Type = ManagedSQObjectType.RefObj,
                RefObjValue = obj,
            };
        }

        public void Push(IntPtr vm)
        {
            switch (Type)
            {
                case ManagedSQObjectType.Null:
                    SquirrelFunctions.pushnull(vm);
                    break;
                case ManagedSQObjectType.Bool:
                    SquirrelFunctions.pushbool(vm, IntValue);
                    break;
                case ManagedSQObjectType.Int:
                    SquirrelFunctions.pushinteger(vm, IntValue);
                    break;
                case ManagedSQObjectType.Float:
                    SquirrelFunctions.pushfloat(vm, FloatValue);
                    break;
                case ManagedSQObjectType.String:
                    SquirrelFunctions.pushstring(vm, StringValue, -1);
                    break;
                case ManagedSQObjectType.Root:
                    SquirrelFunctions.pushroottable(vm);
                    break;
                case ManagedSQObjectType.RefObj:
                    SquirrelFunctions.pushobject(vm, RefObjValue);
                    break;
                case ManagedSQObjectType.Parameter:
                    SquirrelFunctions.push(vm, IntValue);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
