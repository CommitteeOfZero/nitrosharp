using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace NitroSharp.NsScript
{
    public abstract class ConstantValue : IEquatable<ConstantValue>
    {
        private const string UseNullConstError = "ConstantValue.Null should be used instead of 'null'.";

        public static readonly ConstantValue True = new ConstantValueBoolean(true);
        public static readonly ConstantValue False = new ConstantValueBoolean(false);
        public static readonly ConstantValue Zero = new ConstantValueDouble(0, isDeltaValue: false);
        public static readonly ConstantValue DeltaZero = new ConstantValueDouble(0, isDeltaValue: true);
        public static readonly ConstantValue One = new ConstantValueDouble(1, isDeltaValue: false);
        public static readonly ConstantValue DeltaOne = new ConstantValueDouble(1, isDeltaValue: true);
        public static readonly ConstantValue EmptyString = new ConstantValueString(string.Empty);
        public static readonly ConstantValue AtSymbol = new ConstantValueString("@");
        public static readonly ConstantValue Null = new ConstantValueNull();


        public static ConstantValue Create(double value) => Create(value, false);
        public static ConstantValue Create(double value, bool isDeltaValue)
        {
            switch (value)
            {
                case 0:
                    return isDeltaValue ? DeltaZero : Zero;
                case 1:
                    return isDeltaValue ? DeltaOne : One;

                default:
                    return new ConstantValueDouble(value, isDeltaValue);
            }
        }

        public static ConstantValue Create(string value)
        {
            switch (value)
            {
                case null:
                    return ConstantValue.Null;

                case "":
                    return EmptyString;

                case "@":
                    return AtSymbol;

                default:
                    return new ConstantValueString(value);
            }
        }

        public static ConstantValue Create(bool value) => value ? True : False;
        public static ConstantValue Create(BuiltInEnumValue value) => new EnumValueConstant(value);

        public static ConstantValue Default(BuiltInType type)
        {
            switch (type)
            {
                case BuiltInType.Double:
                    return Zero;

                case BuiltInType.String:
                    return EmptyString;

                case BuiltInType.Boolean:
                    return False;

                case BuiltInType.Null:
                default:
                    return ConstantValue.Null;
            }
        }

        private static bool IsNull(ConstantValue value)
        {
            Debug.Assert(!ReferenceEquals(value, null));
            return ReferenceEquals(value, Null);
        }

        private static bool IsDefault(ConstantValue value)
        {
            Debug.Assert(!ReferenceEquals(value, null));
            return ReferenceEquals(value, Default(value.Type));
        }

        public abstract BuiltInType Type { get; }
        public virtual double DoubleValue => throw new InvalidOperationException();
        public virtual string StringValue => throw new InvalidOperationException();
        public virtual bool BooleanValue => throw new InvalidOperationException();
        public virtual bool IsDeltaValue => throw new InvalidOperationException();
        public virtual BuiltInEnumValue EnumValue => throw new InvalidOperationException();

        protected abstract bool TryConvertTo(BuiltInType targetType, out ConstantValue result);
        public ConstantValue ConvertTo(BuiltInType targetType)
        {
            return TryConvertTo(targetType, out var result)
                ? result
                : throw InvalidConversion(Type, targetType);
        }

        protected abstract int GetHashCodeImpl();
        protected virtual bool EqualsImpl(ConstantValue other)
        {
            return ReferenceEquals(this, other);
        }

        public bool Equals(ConstantValue other) => AreEqual(this, other);
        public override bool Equals(object obj) => Equals(obj as ConstantValue);
        public override int GetHashCode() => GetHashCodeImpl();

        private static bool AreEqual(ConstantValue left, ConstantValue right)
        {
            Debug.Assert(!ReferenceEquals(left, null));
            Debug.Assert(!ReferenceEquals(right, null));

            if (ReferenceEquals(left, right) || IsDefault(left) && IsDefault(right))
            {
                return true;
            }

            // A string is never equal to a number.
            if (left.Type == BuiltInType.String ^ right.Type == BuiltInType.String
                && left.Type == BuiltInType.Double ^ right.Type == BuiltInType.Double)
            {
                return false;
            }

            // If just one of the two values is Null, convert it to the other value's type.
            if (left.Type == BuiltInType.Null)
            {
                return left.ConvertTo(right.Type).EqualsImpl(right);
            }
            if (right.Type == BuiltInType.Null)
            {
                return left.EqualsImpl(right.ConvertTo(left.Type));
            }

            // One of the values is an enum value and the other one is a string.
            // Convert the enum value to string and compare the two strings in a non-case sensitive manner.
            // Note: normally string comparsion *is* case sensitive, which is why this special case even exists.
            if (left.Type == BuiltInType.EnumValue ^ right.Type == BuiltInType.EnumValue
                && left.Type == BuiltInType.String ^ right.Type == BuiltInType.String)
            {
                string l = left.ConvertTo(BuiltInType.String).StringValue;
                string r = right.ConvertTo(BuiltInType.String).StringValue;
                return l.Equals(r, StringComparison.OrdinalIgnoreCase);
            }

            bool equal = false;
            if (right.TryConvertTo(left.Type, out var convertedRightValue))
            {
                equal = equal || left.EqualsImpl(convertedRightValue);
            }
            if (left.TryConvertTo(right.Type, out var convertedLeftValue))
            {
                equal = equal || convertedLeftValue.EqualsImpl(right);
            }

            return equal;
        }

        private static ConstantValue Add(ConstantValue left, ConstantValue right)
        {
            // null + null
            if (IsNull(left) && IsNull(right))
            {
                return Zero;
            }

            // double + double / bool + bool / double + bool
            if (left.Type != BuiltInType.String && right.Type != BuiltInType.String)
            {
                return Create(left.ConvertTo(BuiltInType.Double).DoubleValue
                    + right.ConvertTo(BuiltInType.Double).DoubleValue);
            }

            // string + string
            if (left.Type == BuiltInType.String && right.Type == BuiltInType.String)
            {
                return Create(left.ConvertTo(BuiltInType.String).StringValue
                    + right.ConvertTo(BuiltInType.String).StringValue);
            }

            // Special case #1: the left value is "@" and the right value in a double.
            // Results in a so-called 'delta' value.
            if (ReferenceEquals(left, AtSymbol) && right.Type == BuiltInType.Double)
            {
                return Create(right.DoubleValue, isDeltaValue: true);
            }

            // Worst scenario: one of the values is a string and the other one is either a double or a bool.
            ConstantValue stringValue, nonStringValue;
            if (left.Type == BuiltInType.String)
            {
                stringValue = left;
                nonStringValue = right;
            }
            else
            {
                stringValue = right;
                nonStringValue = left;
            }

            // number + "" = number
            if (nonStringValue.Type == BuiltInType.Double && ReferenceEquals(stringValue, EmptyString))
            {
                return nonStringValue;
            }

            bool RepresentsNumber(ConstantValue v) => double.TryParse(v.StringValue, NumberStyles.AllowDecimalPoint, null, out _);
            if (RepresentsNumber(stringValue))
            {
                // Special case #2: The string value represents a number (and the other value is most likely a double).
                // So we have an expression like this: 42 + "3".
                // The string value is simply ignored in such cases, so we can just return the nonStringValue.
                return nonStringValue;
            }

            // Now, if stringValue is just an arbitrary string, the result of the operation should also be a string.
            return Create(left.ConvertTo(BuiltInType.String).StringValue
                + right.ConvertTo(BuiltInType.String).StringValue);
        }

        public static ConstantValue operator ==(ConstantValue left, ConstantValue right)
        {
            return Create(AreEqual(left, right));
        }

        public static ConstantValue operator !=(ConstantValue left, ConstantValue right)
        {
            return Create(!AreEqual(left, right));
        }

        public static ConstantValue operator <(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);
            return Create(left.ConvertTo(BuiltInType.Double).DoubleValue < right.ConvertTo(BuiltInType.Double).DoubleValue);
        }

        public static ConstantValue operator <=(ConstantValue left, ConstantValue right)
        {
            return left < right || left == right;
        }

        public static ConstantValue operator >(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);
            return Create(left.ConvertTo(BuiltInType.Double).DoubleValue > right.ConvertTo(BuiltInType.Double).DoubleValue);
        }

        public static ConstantValue operator >=(ConstantValue left, ConstantValue right)
        {
            return left > right || left == right;
        }

        public static ConstantValue operator +(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);
            return Add(left, right);
        }

        public static ConstantValue operator -(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);

            left = left.ConvertTo(BuiltInType.Double);
            right = right.ConvertTo(BuiltInType.Double);
            double value = left.DoubleValue - right.DoubleValue;
            bool isDelta = left.IsDeltaValue || right.IsDeltaValue;
            return new ConstantValueDouble(value, isDelta);
        }

        public static ConstantValue operator *(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);

            left = left.ConvertTo(BuiltInType.Double);
            right = right.ConvertTo(BuiltInType.Double);
            double value = left.DoubleValue * right.DoubleValue;
            bool isDelta = left.IsDeltaValue || right.IsDeltaValue;
            return Create(value, isDelta);
        }

        public static ConstantValue operator /(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);

            left = left.ConvertTo(BuiltInType.Double);
            right = right.ConvertTo(BuiltInType.Double);
            double value = left.DoubleValue / right.DoubleValue;
            bool isDelta = left.IsDeltaValue || right.IsDeltaValue;
            return Create(value, isDelta);
        }

        public static ConstantValue operator %(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);

            left = left.ConvertTo(BuiltInType.Double);
            right = right.ConvertTo(BuiltInType.Double);
            double value = left.DoubleValue % right.DoubleValue;
            bool isDelta = left.IsDeltaValue || right.IsDeltaValue;
            return Create(value, isDelta);
        }

        public static ConstantValue operator !(ConstantValue value)
        {
            ThrowIfNullReference(value);
            value = value.ConvertTo(BuiltInType.Boolean);
            return Create(!value.BooleanValue);
        }

        public static ConstantValue operator +(ConstantValue value)
        {
            ThrowIfNullReference(value);
            return value.ConvertTo(BuiltInType.Double);
        }

        public static ConstantValue operator -(ConstantValue value)
        {
            ThrowIfNullReference(value);
            value = value.ConvertTo(BuiltInType.Double);
            return Create(-value.DoubleValue, value.IsDeltaValue);
        }

        public static ConstantValue operator ++(ConstantValue value)
        {
            ThrowIfNullReference(value);
            value = value.ConvertTo(BuiltInType.Double);
            return Create(value.DoubleValue + 1, value.IsDeltaValue);
        }

        public static ConstantValue operator --(ConstantValue value)
        {
            ThrowIfNullReference(value);
            value = value.ConvertTo(BuiltInType.Double);
            return Create(value.DoubleValue - 1, value.IsDeltaValue);
        }

        public static bool operator true(ConstantValue value)
        {
            ThrowIfNullReference(value);
            return value.ConvertTo(BuiltInType.Boolean).BooleanValue;
        }

        public static bool operator false(ConstantValue value)
        {
            ThrowIfNullReference(value);
            return !value.ConvertTo(BuiltInType.Boolean).BooleanValue;
        }

        public static ConstantValue operator |(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);

            left = left.ConvertTo(BuiltInType.Boolean);
            right = right.ConvertTo(BuiltInType.Boolean);
            return Create(left.BooleanValue | right.BooleanValue);
        }

        public static ConstantValue operator &(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);

            left = left.ConvertTo(BuiltInType.Boolean);
            right = right.ConvertTo(BuiltInType.Boolean);
            return Create(left.BooleanValue & right.BooleanValue);
        }

        private static void ThrowIfNullReference(ConstantValue value)
        {
            if (ReferenceEquals(value, null))
            {
                throw new InvalidOperationException(UseNullConstError);
            }
        }

        private static void ThrowIfNullReference(ConstantValue a, ConstantValue b)
        {
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                throw new InvalidOperationException(UseNullConstError);
            }
        }

        private static Exception InvalidConversion(BuiltInType from, BuiltInType to)
        {
            throw new InvalidOperationException($"Cannot convert from '{from}' to '{to}'.");
        }

        private sealed class ConstantValueDouble : ConstantValue
        {
            public ConstantValueDouble(double value, bool isDeltaValue = false)
            {
                DoubleValue = value;
                IsDeltaValue = isDeltaValue;
            }

            public override BuiltInType Type => BuiltInType.Double;
            public override double DoubleValue { get; }
            public override bool IsDeltaValue { get; }

            protected override bool EqualsImpl(ConstantValue other)
            {
                return DoubleValue == other.DoubleValue && IsDeltaValue == other.IsDeltaValue;
            }

            protected override bool TryConvertTo(BuiltInType targetType, out ConstantValue result)
            {
                switch (targetType)
                {
                    case BuiltInType.Double:
                        result = this;
                        return true;

                    case BuiltInType.String:
                        int i = (int)DoubleValue;
                        result = Create(i.ToString());
                        return true;

                    case BuiltInType.Boolean:
                        result = Create(DoubleValue > 0);
                        return true;

                    default:
                        result = null;
                        return false;
                }
            }

            protected override int GetHashCodeImpl()
            {
                // Auto-generated by Roslyn.
                int hashCode = 372761160;
                hashCode = hashCode * -1521134295 + DoubleValue.GetHashCode();
                hashCode = hashCode * -1521134295 + IsDeltaValue.GetHashCode();
                return hashCode;
            }

            public override string ToString() => DoubleValue.ToString();
        }

        private sealed class ConstantValueBoolean : ConstantValue
        {
            public ConstantValueBoolean(bool value)
            {
                BooleanValue = value;
            }

            public override BuiltInType Type => BuiltInType.Boolean;
            public override bool BooleanValue { get; }

            protected override bool EqualsImpl(ConstantValue other)
            {
                return BooleanValue == other.BooleanValue;
            }

            protected override bool TryConvertTo(BuiltInType targetType, out ConstantValue result)
            {
                switch (targetType)
                {
                    case BuiltInType.Boolean:
                        result = this;
                        return true;

                    case BuiltInType.Double:
                        result =  BooleanValue ? One : Zero;
                        return true;

                    case BuiltInType.String:
                        result = BooleanValue ? Create("1") : Create("0");
                        return true;

                    default:
                        result = null;
                        return false;
                }
            }

            protected override int GetHashCodeImpl()
            {
                return 688532308 + BooleanValue.GetHashCode();
            }

            public override string ToString() => BooleanValue.ToString();
        }

        private sealed class ConstantValueString : ConstantValue
        {
            public ConstantValueString(string value)
            {
                StringValue = value;
            }

            public override BuiltInType Type => BuiltInType.String;
            public override string StringValue { get; }

            protected override bool EqualsImpl(ConstantValue other)
            {
                return StringValue.Equals(other.StringValue, StringComparison.Ordinal);
            }

            protected override bool TryConvertTo(BuiltInType targetType, out ConstantValue result)
            {
                switch (targetType)
                {
                    case BuiltInType.String:
                        result = this;
                        return true;

                    case BuiltInType.Double:
                        result =  ConvertToNumber();
                        return true;

                    case BuiltInType.Boolean:
                        result = False;
                        return true;

                    default:
                        result = null;
                        return false;
                }
            }

            private ConstantValue ConvertToNumber()
            {
                if (StringValue == "@")
                {
                    return DeltaZero;
                }

                bool success = double.TryParse(StringValue, NumberStyles.AllowDecimalPoint, null, out double result);
                return success ? Create(result) : Zero;
            }

            protected override int GetHashCodeImpl()
            {
                return 861544945 + EqualityComparer<string>.Default.GetHashCode(StringValue);
            }

            public override string ToString() => StringValue;
        }

        private sealed class EnumValueConstant : ConstantValue
        {
            public EnumValueConstant(BuiltInEnumValue value)
            {
                EnumValue = value;
            }

            public override BuiltInType Type => BuiltInType.EnumValue;
            public override BuiltInEnumValue EnumValue { get; }

            protected override bool EqualsImpl(ConstantValue other)
            {
                return EnumValue == other.EnumValue;
            }

            protected override bool TryConvertTo(BuiltInType targetType, out ConstantValue result)
            {
                switch (targetType)
                {
                    case BuiltInType.EnumValue:
                        result = this;
                        return true;

                    case BuiltInType.String:
                        result = Create(EnumValue.ToString());
                        return true;

                    default:
                        result = null;
                        return false;
                }
            }

            protected override int GetHashCodeImpl()
            {
                return -1521134295 + EnumValue.GetHashCode();
            }

            public override string ToString()
            {
                return EnumValue.ToString();
            }
        }

        private sealed class ConstantValueNull : ConstantValue
        {
            public override BuiltInType Type => BuiltInType.Null;
            public override bool BooleanValue => false;
            public override string StringValue => string.Empty;
            public override double DoubleValue => 0.0d;

            protected override bool TryConvertTo(BuiltInType targetType, out ConstantValue result)
            {
                result = Default(targetType);
                return true;
            }

            protected override int GetHashCodeImpl() => 0;
            public override string ToString() => "null";
        }
    }
}
