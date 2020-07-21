using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Misc
{
    [StructLayout(LayoutKind.Explicit)]
    public struct X87Float : IEquatable<X87Float>
    {
        [FieldOffset(0)]
        internal uint Value;

        [FieldOffset(0)]
        private float FloatValue; //Be careful and make it private.

        public static readonly X87Float Zero = 0;
        public static readonly X87Float One = 1;

        public static implicit operator X87Float(int i)
        {
            return X87FloatHelper.FromInt(i);
        }

        public static implicit operator X87Float(float val)
        {
            return new X87Float { FloatValue = val };
        }

        public static bool operator ==(X87Float left, X87Float right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(X87Float left, X87Float right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is X87Float @float && Equals(@float);
        }

        public bool Equals(X87Float other)
        {
            return Value == other.Value; //Not implementing -0==0.
        }

        public override int GetHashCode()
        {
            return -1937169414 + Value.GetHashCode();
        }

        public static X87Float operator +(X87Float left, X87Float right)
        {
            return X87FloatHelper.Add(left, right);
        }

        public static X87Float operator -(X87Float left, X87Float right)
        {
            return X87FloatHelper.Minus(left, right);
        }

        public static X87Float operator *(X87Float left, X87Float right)
        {
            return X87FloatHelper.Multiply(left, right);
        }

        public static X87Float operator /(X87Float left, X87Float right)
        {
            return X87FloatHelper.Divide(left, right);
        }

        public static X87Float operator -(X87Float value)
        {
            return X87FloatHelper.Neg(value);
        }

        public static X87Float Max(X87Float a, X87Float b)
        {
            return X87FloatHelper.Max(a, b);
        }

        public static X87Float Min(X87Float a, X87Float b)
        {
            return X87FloatHelper.Min(a, b);
        }

        public static int Compare(X87Float a, X87Float b)
        {
            return X87FloatHelper.Compare(a, b);
        }

        public static X87Float Abs(X87Float a)
        {
            return X87FloatHelper.Abs(a);
        }

        public static int Sign(X87Float a)
        {
            return X87FloatHelper.Sign(a);
        }

        public float ToFloat()
        {
            return FloatValue;
        }
    }
}
