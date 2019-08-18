using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.Script
{
    public struct SQObject
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

        [StructLayout(LayoutKind.Explicit)]
        public struct SQObjectValue
        {
            [FieldOffset(0)] public IntPtr Pointer;
            [FieldOffset(0)] public int Integer;
            [FieldOffset(0)] public float Float;
        }

        public SQObjectType Type;
        public SQObjectValue Value;

        public static readonly SQObject Null = new SQObject
        {
            Type = SQObjectType.OT_NULL,
            Value = new SQObjectValue { Integer = 0 },
        };

        public override string ToString()
        {
            switch (Type)
            {
                case SQObjectType.OT_NULL:
                    return "null";
                case SQObjectType.OT_INTEGER:
                    return Value.Integer.ToString();
                case SQObjectType.OT_FLOAT:
                    return Value.Float.ToString(CultureInfo.InvariantCulture);
                case SQObjectType.OT_BOOL:
                    return Value.Integer == 0 ? "false" : "true";
                case SQObjectType.OT_STRING:
                    return String.Format("\"{0}\"", Marshal.PtrToStringAnsi(Value.Pointer + 28));
                case SQObjectType.OT_TABLE:
                case SQObjectType.OT_ARRAY:
                case SQObjectType.OT_USERDATA:
                case SQObjectType.OT_CLOSURE:
                case SQObjectType.OT_NATIVECLOSURE:
                case SQObjectType.OT_GENERATOR:
                case SQObjectType.OT_USERPOINTER:
                case SQObjectType.OT_THREAD:
                case SQObjectType.OT_FUNCPROTO:
                case SQObjectType.OT_CLASS:
                case SQObjectType.OT_INSTANCE:
                case SQObjectType.OT_WEAKREF:
                    return string.Empty;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
