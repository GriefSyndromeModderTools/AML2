using AMLCore.Injection.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Misc
{
    internal static class X87FloatHelper
    {
        static X87FloatHelper()
        {
            _add = (BinaryFloatDelegate)Marshal.GetDelegateForFunctionPointer(AddFunc(), typeof(BinaryFloatDelegate));
            _multiply = (BinaryFloatDelegate)Marshal.GetDelegateForFunctionPointer(MulFunc(), typeof(BinaryFloatDelegate));
            _reciprocal = (UnaryFloatDelegate)Marshal.GetDelegateForFunctionPointer(RecFunc(), typeof(UnaryFloatDelegate));
            _fromInt = (UnaryFloatDelegate)Marshal.GetDelegateForFunctionPointer(I2FFunc(), typeof(UnaryFloatDelegate));
        }

        private delegate uint BinaryFloatDelegate(uint a, uint b);
        private delegate uint UnaryFloatDelegate(uint a);

        private static BinaryFloatDelegate _add, _multiply;
        private static UnaryFloatDelegate _reciprocal, _fromInt;

        private static IntPtr AddFunc()
        {
            //push ebp
            //mov ebp, esp
            //push 0
            //fld dword ptr [ebp+8]
            //fld dword ptr [ebp+0xC]
            //faddp
            //fstp dword ptr [esp]
            //pop eax
            //pop ebp
            //ret 8
            return AssemblyCodeStorage.WriteCode(new byte[]
            {
                0x55, 0x89, 0xE5, 0x6A, 0x00, 0xD9, 0x45, 0x08, 0xD9, 0x45, 0x0C, 0xDE, 0xC1, 0xD9, 0x1C, 0x24, 0x58, 0x5D, 0xC2, 0x08, 0x00,
            });
        }

        private static IntPtr MulFunc()
        {
            //push ebp
            //mov ebp, esp
            //push 0
            //fld dword ptr [ebp+8]
            //fld dword ptr [ebp+0xC]
            //fmulp
            //fstp dword ptr [esp]
            //pop eax
            //pop ebp
            //ret 8
            return AssemblyCodeStorage.WriteCode(new byte[]
            {
                0x55, 0x89, 0xE5, 0x6A, 0x00, 0xD9, 0x45, 0x08, 0xD9, 0x45, 0x0C, 0xDE, 0xC9, 0xD9, 0x1C, 0x24, 0x58, 0x5D, 0xC2, 0x08, 0x00,
            });
        }

        private static IntPtr RecFunc()
        {
            //push ebp
            //mov ebp, esp
            //push 0
            //fld1
            //fld dword ptr [ebp+8]
            //fdivp
            //fstp dword ptr [esp]
            //pop eax
            //pop ebp
            //ret 4
            return AssemblyCodeStorage.WriteCode(new byte[]
            {
                0x55, 0x89, 0xE5, 0x6A, 0x00, 0xD9, 0xE8, 0xD9, 0x45, 0x08, 0xDE, 0xF9, 0xD9, 0x1C, 0x24, 0x58, 0x5D, 0xC2, 0x04, 0x00,
            });
        }

        private static IntPtr I2FFunc()
        {
            //push ebp
            //mov ebp, esp
            //push 0
            //fild dword ptr[ebp + 8]
            //fstp dword ptr[esp]
            //pop eax
            //pop ebp
            //ret 4
            return AssemblyCodeStorage.WriteCode(new byte[]
            {
                //0x55, 0x89, 0xE5, 0x6A, 0x00, 0xD9, 0x45, 0x08, 0xD9, 0xE8, 0xDE, 0xF9, 0xD9, 0x1C, 0x24, 0x58, 0x5D, 0xC2, 0x08, 0x00,
                0x55, 0x89, 0xE5, 0x6A, 0x00, 0xDB, 0x45, 0x08, 0xD9, 0xEE, 0xDE, 0xC1, 0xD9, 0x1C, 0x24, 0x58, 0x5D, 0xC2, 0x04, 0x00
            });
        }

        public static X87Float Add(X87Float a, X87Float b)
        {
            return new X87Float { Value = _add(a.Value, b.Value) };
        }

        public static X87Float Neg(X87Float a)
        {
            return new X87Float { Value = a.Value ^ 0x80000000u };
        }

        public static X87Float Minus(X87Float a, X87Float b)
        {
            return Add(a, Neg(b));
        }

        public static X87Float Multiply(X87Float a, X87Float b)
        {
            return new X87Float { Value = _multiply(a.Value, b.Value) };
        }

        public static X87Float Reciprocal(X87Float a)
        {
            return new X87Float { Value = _reciprocal(a.Value) };
        }

        public static X87Float Divide(X87Float a, X87Float b)
        {
            return Multiply(a, Reciprocal(b));
        }

        public static int Compare(X87Float a, X87Float b)
        {
            //Assume no NAN, -0 problems.
            var sa = Math.Sign((int)a.Value);
            var sb = Math.Sign((int)b.Value);
            if (sa != sb) return Math.Sign(sa - sb);
            return sa * Math.Sign(Math.Abs((int)a.Value) - Math.Abs((int)b.Value));
        }

        public static X87Float Max(X87Float a, X87Float b)
        {
            return Compare(a, b) > 0 ? a : b;
        }

        public static X87Float Min(X87Float a, X87Float b)
        {
            return Compare(a, b) < 0 ? a : b;
        }

        public static X87Float Abs(X87Float a)
        {
            return new X87Float { Value = a.Value & 0x7FFFFFFF }; //Erase sign bit.
        }

        public static int Sign(X87Float a)
        {
            return Math.Sign((int)a.Value);
        }

        public static X87Float FromInt(int val)
        {
            return new X87Float { Value = _fromInt((uint)val) };
        }
    }
}
