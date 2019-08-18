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
            Int,
            Float,
            String,
        }

        public ManagedSQObjectType Type;
        public int IntValue;
        public float FloatValue;
        public string StringValue;

        public static readonly ManagedSQObject Null = new ManagedSQObject
        {
            Type = ManagedSQObjectType.Null,
        };

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

        public void Push(IntPtr vm)
        {
            switch (Type)
            {
                case ManagedSQObjectType.Null:
                    SquirrelFunctions.pushnull(vm);
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
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
