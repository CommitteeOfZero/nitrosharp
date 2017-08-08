using System;

namespace NitroSharp.NsScript
{
    public enum NsBuiltInType
    {
        Integer,
        String,
        Boolean,
        Null
    }

    public abstract class ConstantValue : Expression, IEquatable<ConstantValue>
    {
        private const string UseNullConstError = "ConstantValue.Null should be used instead of 'null'.";

        public static readonly ConstantValue True = new ConstantValueBoolean(true);
        public static readonly ConstantValue False = new ConstantValueBoolean(false);
        public static readonly ConstantValue Zero = new ConstantValueInteger(0, isDeltaValue: false);
        public static readonly ConstantValue DeltaZero = new ConstantValueInteger(0, isDeltaValue: true);
        public static readonly ConstantValue One = new ConstantValueInteger(1, isDeltaValue: false);
        public static readonly ConstantValue DeltaOne = new ConstantValueInteger(1, isDeltaValue: true);
        public static readonly ConstantValue EmptyString = new ConstantValueString(string.Empty);
        public static readonly ConstantValue AtSymbol = new ConstantValueString("@");
        public static readonly ConstantValue Null = new ConstantValueNull();

        public static ConstantValue Create(int value, bool isDeltaValue)
        {
            switch (value)
            {
                case 0:
                    return isDeltaValue ? DeltaZero : Zero;

                case 1:
                    return isDeltaValue ? DeltaOne : One;

                default:
                    return new ConstantValueInteger(value, isDeltaValue);
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

        public static ConstantValue Create(object value, bool isDeltaIntegerValue = false)
        {
            switch (value)
            {
                case 0:
                    return isDeltaIntegerValue ? DeltaZero : Zero;

                case 1:
                    return isDeltaIntegerValue ? DeltaOne : One;

                case null:
                    return ConstantValue.Null;

                case true:
                    return True;

                case false:
                    return False;

                case "":
                    return EmptyString;

                case "@":
                    return AtSymbol;

                case int i:
                    return new ConstantValueInteger(i, isDeltaIntegerValue);

                case string s:
                    return new ConstantValueString(s);

                default:
                    throw new ArgumentException("Illegal value.", nameof(value));
            }
        }

        public static ConstantValue Default(NsBuiltInType type)
        {
            switch (type)
            {
                case NsBuiltInType.Integer:
                    return Zero;

                case NsBuiltInType.String:
                    return EmptyString;

                case NsBuiltInType.Boolean:
                    return False;

                case NsBuiltInType.Null:
                default:
                    return ConstantValue.Null;
            }
        }

        public abstract NsBuiltInType Type { get; }
        public abstract object RawValue { get; }
        public virtual int IntegerValue => throw new InvalidOperationException();
        public virtual string StringValue => throw new InvalidOperationException();
        public virtual bool BooleanValue => throw new InvalidOperationException();
        public virtual bool IsDeltaIntegerValue => throw new InvalidOperationException();

        public override SyntaxNodeKind Kind => SyntaxNodeKind.ConstantValue;

        public abstract ConstantValue ConvertTo(NsBuiltInType targetType);

        protected virtual bool EqualsImpl(ConstantValue other)
        {
            return ReferenceEquals(this, other);
        }

        public bool Equals(ConstantValue other)
        {
            return EqualsStatic(this, other);
        }

        private static bool EqualsStatic(ConstantValue left, ConstantValue right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            // If just one of the two values is null, convert it to the other value's type.
            if (left.Type == NsBuiltInType.Null)
            {
                return left.ConvertTo(right.Type).EqualsImpl(right);
            }
            else if (right.Type == NsBuiltInType.Null)
            {
                return left.EqualsImpl(right.ConvertTo(left.Type));
            }

            // If one of the values is a string and the other one is an integer, both should be converted to the integer type.
            if (left.Type == NsBuiltInType.String ^ right.Type == NsBuiltInType.String
                && left.Type == NsBuiltInType.Integer ^ right.Type == NsBuiltInType.Integer)
            {
                return left.ConvertTo(NsBuiltInType.Integer).EqualsImpl(right.ConvertTo(NsBuiltInType.Integer));
            }

            return left.ConvertTo(left.Type).EqualsImpl(right.ConvertTo(left.Type))
                || left.ConvertTo(right.Type).EqualsImpl(right.ConvertTo(right.Type));
        }

        private static ConstantValue OpAdditionStatic(ConstantValue left, ConstantValue right)
        {
            // null + null
            if (left == ConstantValue.Null && right == ConstantValue.Null)
            {
                return Zero;
            }

            // int + int / bool + bool
            if (left.Type != NsBuiltInType.String && right.Type != NsBuiltInType.String)
            {
                return Create(left.ConvertTo(NsBuiltInType.Integer).IntegerValue
                    + right.ConvertTo(NsBuiltInType.Integer).IntegerValue);
            }

            // string + string
            if (left.Type == NsBuiltInType.String && right.Type == NsBuiltInType.String)
            {
                return Create(left.ConvertTo(NsBuiltInType.String).StringValue
                    + right.ConvertTo(NsBuiltInType.String).StringValue);
            }

            // Special case #1: the left value is "@" and the right value in an integer.
            // Results in a so-called 'delta' integer value.
            if (ReferenceEquals(left, AtSymbol) && right.Type == NsBuiltInType.Integer)
            {
                return Create(right.IntegerValue, isDeltaIntegerValue: true);
            }

            // Worst scenario: one of the values is a string and the other one is not.
            ConstantValue stringValue, nonStringValue;
            if (left.Type == NsBuiltInType.String)
            {
                stringValue = left;
                nonStringValue = right;
            }
            else
            {
                stringValue = right;
                nonStringValue = left;
            }

            // int + ""
            if (stringValue == EmptyString)
            {
                return nonStringValue.ConvertTo(NsBuiltInType.Integer);
            }

            bool RepresentsNumber(ConstantValue v) => int.TryParse(v.StringValue, out _);
            if (RepresentsNumber(stringValue))
            {
                // Special case #2: The string value represents a number, and the other value is likely an integer.
                // So we have an expression like this: 42 + "3".
                // According to the rules of the language, the "3" in this case should be converted to an integer.
                // Spoiler: this conversion always results in a zero.
                // So we can just return the nonStringValue (that would be 42 in the example below).

                return nonStringValue.ConvertTo(NsBuiltInType.Integer);
            }

            // Now, if stringValue is just an arbitrary string, the result of the operation should also be a string.
            return Create(left.ConvertTo(NsBuiltInType.String).StringValue
                + right.ConvertTo(NsBuiltInType.String).StringValue);
        }

        public static ConstantValue operator ==(ConstantValue left, ConstantValue right)
        {
            return Create(EqualsStatic(left, right));
        }

        public static ConstantValue operator !=(ConstantValue left, ConstantValue right)
        {
            return Create(!EqualsStatic(left, right));
        }

        public static ConstantValue operator <(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);
            return Create(left.ConvertTo(NsBuiltInType.Integer).IntegerValue < right.ConvertTo(NsBuiltInType.Integer).IntegerValue);
        }

        public static ConstantValue operator <=(ConstantValue left, ConstantValue right)
        {
            return left < right || left == right;
        }

        public static ConstantValue operator >(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);
            return Create(left.ConvertTo(NsBuiltInType.Integer).IntegerValue > right.ConvertTo(NsBuiltInType.Integer).IntegerValue);
        }

        public static ConstantValue operator >=(ConstantValue left, ConstantValue right)
        {
            return left > right || left == right;
        }

        public static ConstantValue operator +(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);
            return OpAdditionStatic(left, right);
        }

        public static ConstantValue operator -(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);

            left = left.ConvertTo(NsBuiltInType.Integer);
            right = right.ConvertTo(NsBuiltInType.Integer);
            int value = left.IntegerValue - right.IntegerValue;
            bool isDelta = left.IsDeltaIntegerValue || right.IsDeltaIntegerValue;
            return new ConstantValueInteger(value, isDelta);
        }

        public static ConstantValue operator *(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);

            left = left.ConvertTo(NsBuiltInType.Integer);
            right = right.ConvertTo(NsBuiltInType.Integer);
            int value = left.IntegerValue * right.IntegerValue;
            bool isDelta = left.IsDeltaIntegerValue || right.IsDeltaIntegerValue;
            return Create(value, isDelta);
        }

        public static ConstantValue operator /(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);

            left = left.ConvertTo(NsBuiltInType.Integer);
            right = right.ConvertTo(NsBuiltInType.Integer);
            int value = left.IntegerValue / right.IntegerValue;
            bool isDelta = left.IsDeltaIntegerValue || right.IsDeltaIntegerValue;
            return Create(value, isDelta);
        }

        public static ConstantValue operator !(ConstantValue value)
        {
            ThrowIfNullReference(value);
            value = value.ConvertTo(NsBuiltInType.Boolean);
            return Create(!value.BooleanValue);
        }

        public static ConstantValue operator +(ConstantValue value)
        {
            ThrowIfNullReference(value);
            return value.ConvertTo(NsBuiltInType.Integer);
        }

        public static ConstantValue operator -(ConstantValue value)
        {
            ThrowIfNullReference(value);
            value = value.ConvertTo(NsBuiltInType.Integer);
            return Create(-value.IntegerValue, value.IsDeltaIntegerValue);
        }

        public static ConstantValue operator++(ConstantValue value)
        {
            ThrowIfNullReference(value);
            value = value.ConvertTo(NsBuiltInType.Integer);
            return Create(value.IntegerValue + 1, value.IsDeltaIntegerValue);
        }

        public static ConstantValue operator --(ConstantValue value)
        {
            ThrowIfNullReference(value);
            value = value.ConvertTo(NsBuiltInType.Integer);
            return Create(value.IntegerValue - 1, value.IsDeltaIntegerValue);
        }

        public static bool operator true(ConstantValue value)
        {
            ThrowIfNullReference(value);
            return value.ConvertTo(NsBuiltInType.Boolean).BooleanValue;
        }

        public static bool operator false(ConstantValue value)
        {
            ThrowIfNullReference(value);
            return !value.ConvertTo(NsBuiltInType.Boolean).BooleanValue;
        }

        public static ConstantValue operator |(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);

            left = left.ConvertTo(NsBuiltInType.Boolean);
            right = right.ConvertTo(NsBuiltInType.Boolean);
            return Create(left.BooleanValue | right.BooleanValue);
        }

        public static ConstantValue operator &(ConstantValue left, ConstantValue right)
        {
            ThrowIfNullReference(left, right);

            left = left.ConvertTo(NsBuiltInType.Boolean);
            right = right.ConvertTo(NsBuiltInType.Boolean);
            return Create(left.BooleanValue & right.BooleanValue);
        }

        public TResult As<TResult>()
        {
            if (Type == NsBuiltInType.Integer && typeof(TResult) == typeof(bool))
            {
                object b = (int)RawValue > 0;
                return (TResult)b;
            }

            return Type == NsBuiltInType.Null ? default(TResult) : (TResult)RawValue;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConstantValue);
        }

        public override int GetHashCode()
        {
            return 17 * 29 + RawValue.GetHashCode();
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

        public override string ToString()
        {
            return RawValue?.ToString() ?? "null";
        }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitConstantValue(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitConstantValue(this);
        }

        private static Exception EqualityNotDefined(NsBuiltInType type1, NsBuiltInType type2)
        {
            return new InvalidOperationException($"Equality is not defined for the types '{type1}' and '{type2}'");
        }

        private static Exception InvalidConversion(NsBuiltInType from, NsBuiltInType to)
        {
            throw new InvalidOperationException($"Conversion from type '{from}' to '{to}' is not valid.");
        }

        private sealed class ConstantValueInteger : ConstantValue
        {
            public ConstantValueInteger(int value, bool isDeltaValue)
            {
                IntegerValue = value;
                IsDeltaIntegerValue = isDeltaValue;
            }

            public override NsBuiltInType Type => NsBuiltInType.Integer;
            public override object RawValue => IntegerValue;
            public override int IntegerValue { get; }
            public override bool IsDeltaIntegerValue { get; }

            protected override bool EqualsImpl(ConstantValue other)
            {
                return IntegerValue == other.IntegerValue && IsDeltaIntegerValue == other.IsDeltaIntegerValue;
            }

            public override ConstantValue ConvertTo(NsBuiltInType targetType)
            {
                switch (targetType)
                {
                    case NsBuiltInType.Integer:
                        return this;

                    case NsBuiltInType.String:
                        return Create(IntegerValue.ToString());

                    case NsBuiltInType.Boolean:
                        return Create(IntegerValue > 0);

                    case NsBuiltInType.Null:
                    default:
                        throw InvalidConversion(Type, targetType);
                }
            }
        }

        private sealed class ConstantValueBoolean : ConstantValue
        {
            public ConstantValueBoolean(bool value)
            {
                BooleanValue = value;
            }

            public override NsBuiltInType Type => NsBuiltInType.Boolean;
            public override object RawValue => BooleanValue;
            public override bool BooleanValue { get; }

            protected override bool EqualsImpl(ConstantValue other)
            {
                return BooleanValue == other.BooleanValue;
            }

            public override ConstantValue ConvertTo(NsBuiltInType targetType)
            {
                switch (targetType)
                {
                    case NsBuiltInType.Boolean:
                        return this;

                    case NsBuiltInType.Integer:
                        return BooleanValue ? One : Zero;

                    case NsBuiltInType.String:
                        return BooleanValue ? Create("1") : Create("0");

                    default:
                        throw InvalidConversion(Type, targetType);
                }
            }
        }

        private sealed class ConstantValueString : ConstantValue
        {
            public ConstantValueString(string value)
            {
                StringValue = value;
            }

            public override NsBuiltInType Type => NsBuiltInType.String;
            public override object RawValue => StringValue;
            public override string StringValue { get; }

            protected override bool EqualsImpl(ConstantValue other)
            {
                return StringValue.Equals(other.StringValue, StringComparison.Ordinal);
            }

            public override ConstantValue ConvertTo(NsBuiltInType targetType)
            {
                switch (targetType)
                {
                    case NsBuiltInType.String:
                        return this;

                    case NsBuiltInType.Integer:
                        return StringValue == "@" ? DeltaZero : Zero;

                    case NsBuiltInType.Boolean:
                        return False;

                    case NsBuiltInType.Null:
                    default:
                        throw InvalidConversion(Type, targetType);
                }
            }
        }

        private sealed class ConstantValueNull : ConstantValue
        {
            public override NsBuiltInType Type => NsBuiltInType.Null;
            public override object RawValue => null;

            public override ConstantValue ConvertTo(NsBuiltInType targetType)
            {
                return Default(targetType);
            }
        }
    }
}
