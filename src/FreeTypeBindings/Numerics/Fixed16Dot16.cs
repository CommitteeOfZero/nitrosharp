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
        public static readonly Fixed16Dot16 AnglePI = new Fixed16Dot16(180);
        public static readonly Fixed16Dot16 Angle2PI = new Fixed16Dot16(360);
        public static readonly Fixed16Dot16 AnglePI2 = new Fixed16Dot16(90);
        public static readonly Fixed16Dot16 AnglePI4 = new Fixed16Dot16(45);

        public Fixed16Dot16(int value)
        {
            Value = value << 16;
        }

        public Fixed16Dot16(float value)
        {
            Value = (int)(value * 65536);
        }

        public Fixed16Dot16(double value)
        {
            Value = (int)(value * 65536);
        }

        public Fixed16Dot16(decimal value)
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

        public static Fixed16Dot16 Add(Fixed16Dot16 left, Fixed16Dot16 right) => FromRawValue(left.Value + right.Value);
        public static Fixed16Dot16 Subtract(Fixed16Dot16 left, Fixed16Dot16 right) => FromRawValue(left.Value - right.Value);

        public static Fixed16Dot16 Multiply(Fixed16Dot16 left, Fixed16Dot16 right)
        {
            long mul = (long)left.Value * (long)right.Value;
            Fixed16Dot16 ans = new Fixed16Dot16();
            ans.Value = (int)(mul >> 16);
            return ans;
        }

        /// <summary>
        /// A very simple function used to perform the computation ‘(a*b)/0x10000’ with maximal accuracy. Most of the
        /// time this is used to multiply a given value by a 16.16 fixed float factor.
        /// </summary>
        /// <remarks><para>
        /// NOTE: This is a native FreeType function.
        /// </para><para>
        /// This function has been optimized for the case where the absolute value of ‘a’ is less than 2048, and ‘b’ is
        /// a 16.16 scaling factor. As this happens mainly when scaling from notional units to fractional pixels in
        /// FreeType, it resulted in noticeable speed improvements between versions 2.x and 1.x.
        /// </para><para>
        /// As a conclusion, always try to place a 16.16 factor as the second argument of this function; this can make
        /// a great difference.
        /// </para></remarks>
        /// <param name="a">The first multiplier.</param>
        /// <param name="b">The second multiplier. Use a 16.16 factor here whenever possible (see note below).</param>
        /// <returns>The result of ‘(a*b)/0x10000’.</returns>
        public static Fixed16Dot16 MultiplyFix(int a, Fixed16Dot16 b)
        {
            return FromRawValue((int)FT.FT_MulFix((IntPtr)a, (IntPtr)b.Value));
        }

        public static Fixed16Dot16 Divide(Fixed16Dot16 left, Fixed16Dot16 right)
        {
            long div = ((long)left.Value << 16) / right.Value;
            Fixed16Dot16 ans = new Fixed16Dot16();
            ans.Value = (int)div;
            return ans;
        }

        /// <summary>
        /// A very simple function used to perform the computation ‘(a*0x10000)/b’ with maximal accuracy. Most of the
        /// time, this is used to divide a given value by a 16.16 fixed float factor.
        /// </summary>
        /// <remarks><para>
        /// NOTE: This is a native FreeType function.
        /// </para><para>
        /// The optimization for <see cref="DivideFix"/> is simple: If (a &lt;&lt; 16) fits in 32 bits, then the division
        /// is computed directly. Otherwise, we use a specialized version of <see cref="MultiplyDivide"/>.
        /// </para></remarks>
        /// <param name="a">The first multiplier.</param>
        /// <param name="b">The second multiplier. Use a 16.16 factor here whenever possible (see note below).</param>
        /// <returns>The result of ‘(a*0x10000)/b’.</returns>
        public static Fixed16Dot16 DivideFix(int a, Fixed16Dot16 b)
        {
            return FromRawValue((int)FT.FT_DivFix((IntPtr)a, (IntPtr)b.Value));
        }

        /// <summary><para>
        /// A very simple function used to perform the computation ‘(a*b)/c’ with maximal accuracy (it uses a 64-bit
        /// intermediate integer whenever necessary).
        /// </para><para>
        /// This function isn't necessarily as fast as some processor specific operations, but is at least completely
        /// portable.
        /// </para></summary>
        /// <remarks>This is a native FreeType function.</remarks>
        /// <param name="a">The first multiplier.</param>
        /// <param name="b">The second multiplier.</param>
        /// <param name="c">The divisor.</param>
        /// <returns>
        /// The result of ‘(a*b)/c’. This function never traps when trying to divide by zero; it simply returns
        /// ‘MaxInt’ or ‘MinInt’ depending on the signs of ‘a’ and ‘b’.
        /// </returns>
        public static Fixed16Dot16 MultiplyDivide(Fixed16Dot16 a, Fixed16Dot16 b, Fixed16Dot16 c)
        {
            return FromRawValue((int)FT.FT_MulDiv((IntPtr)a.Value, (IntPtr)b.Value, (IntPtr)c.Value));
        }

        public static Fixed16Dot16 Atan2(Fixed16Dot16 x, Fixed16Dot16 y)
        {
            return FromRawValue((int)FT.FT_Atan2((IntPtr)x.Value, (IntPtr)y.Value));
        }

        public static Fixed16Dot16 AngleDiff(Fixed16Dot16 angle1, Fixed16Dot16 angle2)
        {
            return FromRawValue((int)FT.FT_Angle_Diff((IntPtr)angle1.Value, (IntPtr)angle2.Value));
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

        public int Floor()
        {
            return Value >> 16;
        }

        public Fixed16Dot16 FloorFix()
        {
            //TODO does the P/Invoke overhead make this slower than re-implementing in C#? Test it
            return FromRawValue((int)FT.FT_FloorFix((IntPtr)Value));
        }

        public int Round()
        {
            //add 2^15, rounds the integer part up if the decimal value is >= 0.5
            return (Value + 32768) >> 16;
        }

        /// <summary>
        /// A very simple function used to round a 16.16 fixed number.
        /// </summary>
        /// <remarks>This is a native FreeType function.</remarks>
        /// <returns>The result of ‘(a + 0x8000) &amp; -0x10000’.</returns>
        public Fixed16Dot16 RoundFix()
        {
            return FromRawValue((int)FT.FT_RoundFix((IntPtr)Value));
        }

        public int Ceiling()
        {
            //add 2^16 - 1, rounds the integer part up if there's any decimal value
            return (Value + 65535) >> 16;
        }

        public Fixed16Dot16 CeilingFix() => FromRawValue((int)FT.FT_CeilFix((IntPtr)Value));
        public Fixed16Dot16 Sin() => FromRawValue((int)FT.FT_Sin((IntPtr)Value));
        public Fixed16Dot16 Cos() => FromRawValue((int)FT.FT_Cos((IntPtr)Value));
        public Fixed16Dot16 Tan() => FromRawValue((int)FT.FT_Tan((IntPtr)Value));

        public int ToInt32() => Floor();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ToSingle() => Value / 65536f;

        public double ToDouble() => Value / 65536d;
        public decimal ToDecimal() => Value / 65536m;

        public bool Equals(Fixed16Dot16 other) => Value == other.Value;
        public int CompareTo(Fixed16Dot16 other) => Value.CompareTo(other.Value);

        public string ToString(IFormatProvider provider) => ToDecimal().ToString(provider);
        public string ToString(string format) => ToDecimal().ToString(format);
        public string ToString(string format, IFormatProvider provider)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            return ToDecimal().ToString(format, provider);
        }

        public override string ToString() => ToDecimal().ToString();
        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is Fixed16Dot16 fp)
            {
                return Equals(fp);
            }
            else if (obj is int)
            {
                return Value == ((Fixed16Dot16)obj).Value;
            }

            return false;
        }
    }
}
