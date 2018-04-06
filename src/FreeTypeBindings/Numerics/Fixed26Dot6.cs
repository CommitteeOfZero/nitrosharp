﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    /// <summary>
    /// Represents a fixed-point decimal value with 6 bits of decimal precision.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Fixed26Dot6 : IEquatable<Fixed26Dot6>, IComparable<Fixed26Dot6>
    {
        public Fixed26Dot6(int value)
        {
            Value = value << 6;
        }

        public Fixed26Dot6(float value)
        {
            Value = (int)(value * 64);
        }

        public static Fixed26Dot6 FromRawValue(int value)
        {
            var f = new Fixed26Dot6();
            f.Value = value;
            return f;
        }

        public static Fixed26Dot6 FromInt32(int value)
        {
            return new Fixed26Dot6(value);
        }

        public static Fixed26Dot6 FromSingle(float value)
        {
            return new Fixed26Dot6(value);
        }

        public static Fixed26Dot6 FromDouble(double value)
        {
            return new Fixed26Dot6(value);
        }

        public static Fixed26Dot6 FromDecimal(decimal value)
        {
            return new Fixed26Dot6(value);
        }

        public Fixed26Dot6(double value)
        {
            Value = (int)(value * 64);
        }

        public Fixed26Dot6(decimal value)
        {
            Value = (int)(value * 64);
        }

        public int Value { get; private set; }

        public static implicit operator Fixed26Dot6(short value) => new Fixed26Dot6(value);
        public static implicit operator Fixed26Dot6(int value) => new Fixed26Dot6(value);
        public static implicit operator Fixed26Dot6(float value) => new Fixed26Dot6(value);
        public static implicit operator Fixed26Dot6(double value) => new Fixed26Dot6(value);
        public static implicit operator Fixed26Dot6(decimal value) => new Fixed26Dot6(value);
        public static explicit operator int(Fixed26Dot6 value) => value.ToInt32();
        public static explicit operator float(Fixed26Dot6 value) => value.ToSingle();
        public static implicit operator double(Fixed26Dot6 value) => value.ToDouble();
        public static implicit operator decimal(Fixed26Dot6 value) => value.ToDecimal();
        public static bool operator ==(Fixed26Dot6 left, Fixed26Dot6 right) => left.Equals(right);
        public static bool operator !=(Fixed26Dot6 left, Fixed26Dot6 right) => !(left == right);
        public static bool operator <(Fixed26Dot6 left, Fixed26Dot6 right) => left.CompareTo(right) < 0;
        public static bool operator <=(Fixed26Dot6 left, Fixed26Dot6 right) => left.CompareTo(right) <= 0;
        public static bool operator >(Fixed26Dot6 left, Fixed26Dot6 right) => left.CompareTo(right) > 0;
        public static bool operator >=(Fixed26Dot6 left, Fixed26Dot6 right) => left.CompareTo(right) >= 0;

        public int Floor()
        {
            return Value >> 6;
        }

        public int Round()
        {
            // Add 2^5, rounds the integer part up if the decimal value is >= 0.5
            return (Value + 32) >> 6;
        }

        public int Ceiling()
        {
            // Add 2^6 - 1, rounds the integer part up if there's any decimal value
            return (Value + 63) >> 6;
        }

        public int ToInt32() => Floor();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ToSingle() => Value / 64f;

        public double ToDouble() => Value / 64d;
        public decimal ToDecimal() => Value / 64m;

        public bool Equals(Fixed26Dot6 other) => Value == other.Value;
        public int CompareTo(Fixed26Dot6 other) => Value.CompareTo(other.Value);

        public string ToString(IFormatProvider provider) => ToDecimal().ToString(provider);
        public string ToString(string format) => ToDecimal().ToString(format);
        public string ToString(string format, IFormatProvider provider) => ToDecimal().ToString(format, provider);
        public override string ToString() => ToDecimal().ToString();

        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is Fixed26Dot6 fp)
            {
                return Equals(fp);
            }
            else if (obj is int)
            {
                return Value == ((Fixed26Dot6)obj).Value;
            }

            return false;
        }
    }
}
