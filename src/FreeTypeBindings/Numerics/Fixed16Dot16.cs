using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    /// <summary>
    /// Represents a fixed-point decimal value with 16 bits of decimal precision.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Fixed16Dot16 : IEquatable<Fixed16Dot16>, IComparable<Fixed16Dot16>
    {
        private static readonly Fixed16Dot16 AnglePI = new Fixed16Dot16(180);
        private static readonly Fixed16Dot16 Angle2PI = new Fixed16Dot16(360);
        private static readonly Fixed16Dot16 AnglePI2 = new Fixed16Dot16(90);
        private static readonly Fixed16Dot16 AnglePI4 = new Fixed16Dot16(45);

        private Fixed16Dot16(int value)
        {
            Value = value << 16;
        }

        private Fixed16Dot16(float value)
        {
            Value = (int)(value * 65536);
        }

        private Fixed16Dot16(double value)
        {
            Value = (int)(value * 65536);
        }

        private Fixed16Dot16(decimal value)
        {
            Value = (int)(value * 65536);
        }

        public int Value { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed16Dot16 FromRawValue(int value)
        {
            Fixed16Dot16 f = new Fixed16Dot16();
            f.Value = value;
            return f;
        }

        public static Fixed16Dot16 FromInt32(int value) => new Fixed16Dot16(value);
        public static Fixed16Dot16 FromSingle(float value) => new Fixed16Dot16(value);
        public static Fixed16Dot16 FromDouble(double value) => new Fixed16Dot16(value);
        public static Fixed16Dot16 FromDecimal(decimal value) => new Fixed16Dot16(value);

        private static Fixed16Dot16 Add(Fixed16Dot16 left, Fixed16Dot16 right) => FromRawValue(left.Value + right.Value);
        private static Fixed16Dot16 Subtract(Fixed16Dot16 left, Fixed16Dot16 right) => FromRawValue(left.Value - right.Value);

        private static Fixed16Dot16 Multiply(Fixed16Dot16 left, Fixed16Dot16 right)
        {
            long mul = (long)left.Value * (long)right.Value;
            Fixed16Dot16 ans = new Fixed16Dot16();
            ans.Value = (int)(mul >> 16);
            return ans;
        }

        private static Fixed16Dot16 Divide(Fixed16Dot16 left, Fixed16Dot16 right)
        {
            long div = ((long)left.Value << 16) / right.Value;
            Fixed16Dot16 ans = new Fixed16Dot16();
            ans.Value = (int)div;
            return ans;
        }

        public static implicit operator Fixed16Dot16(short value) => new Fixed16Dot16(value);
        public static explicit operator Fixed16Dot16(int value) => new Fixed16Dot16(value);
        public static explicit operator Fixed16Dot16(float value) => new Fixed16Dot16(value);
        public static explicit operator Fixed16Dot16(double value) => new Fixed16Dot16(value);
        public static explicit operator Fixed16Dot16(decimal value) => new Fixed16Dot16(value);

        public static explicit operator int(Fixed16Dot16 value) => value.ToInt32();
        public static explicit operator float(Fixed16Dot16 value) => value.ToSingle();
        public static implicit operator double(Fixed16Dot16 value) => value.ToDouble();
        public static implicit operator decimal(Fixed16Dot16 value) => value.ToDecimal();

        public static Fixed16Dot16 operator +(Fixed16Dot16 left, Fixed16Dot16 right) => Add(left, right);
        public static Fixed16Dot16 operator -(Fixed16Dot16 left, Fixed16Dot16 right) => Subtract(left, right);
        public static Fixed16Dot16 operator *(Fixed16Dot16 left, Fixed16Dot16 right) => Multiply(left, right);
        public static Fixed16Dot16 operator /(Fixed16Dot16 left, Fixed16Dot16 right) => Divide(left, right);
        public static bool operator ==(Fixed16Dot16 left, Fixed16Dot16 right) => left.Equals(right);
        public static bool operator !=(Fixed16Dot16 left, Fixed16Dot16 right) => !(left == right);
        public static bool operator <(Fixed16Dot16 left, Fixed16Dot16 right) => left.CompareTo(right) < 0;
        public static bool operator <=(Fixed16Dot16 left, Fixed16Dot16 right) => left.CompareTo(right) <= 0;
        public static bool operator >(Fixed16Dot16 left, Fixed16Dot16 right) => left.CompareTo(right) > 0;
        public static bool operator >=(Fixed16Dot16 left, Fixed16Dot16 right) => left.CompareTo(right) >= 0;

        private int Floor()
        {
            return Value >> 16;
        }

        public int ToInt32() => Floor();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ToSingle() => Value / 65536f;

        public double ToDouble() => Value / 65536d;
        public decimal ToDecimal() => Value / 65536m;

        public bool Equals(Fixed16Dot16 other) => Value == other.Value;
        public int CompareTo(Fixed16Dot16 other) => Value.CompareTo(other.Value);

        public override string ToString() => ToDecimal().ToString();
        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is Fixed16Dot16 fp)
            {
                return Equals(fp);
            }
            if (obj is int)
            {
                return Value == ((Fixed16Dot16)obj).Value;
            }

            return false;
        }
    }
}
