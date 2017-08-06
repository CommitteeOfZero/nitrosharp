using System;
using System.Diagnostics;

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
        public static readonly ConstantValue True = new ConstantValueBoolean(true);
        public static readonly ConstantValue False = new ConstantValueBoolean(false);
        public static readonly ConstantValue Zero = new ConstantValueInteger(0, isDeltaValue: false);
        public static readonly ConstantValue DeltaZero = new ConstantValueInteger(0, isDeltaValue: true);
        public static readonly ConstantValue One = new ConstantValueInteger(1, isDeltaValue: false);
        public static readonly ConstantValue DeltaOne = new ConstantValueInteger(1, isDeltaValue: true);
        public static readonly ConstantValue EmptyString = new ConstantValueString(string.Empty);
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
            if (value == null)
            {
                return Null;
            }

            return value == string.Empty ? EmptyString : new ConstantValueString(value);
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
                    return Null;

                case true:
                    return True;

                case false:
                    return False;

                case "":
                    return EmptyString;

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
                    return Null;
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
        public abstract ConstantValue Add(ConstantValue valueToAdd);

        public TResult As<TResult>()
        {
            if (Type == NsBuiltInType.Integer && typeof(TResult) == typeof(bool))
            {
                object b = (int)RawValue > 0;
                return (TResult)b;
            }

            return Type == NsBuiltInType.Null ? default(TResult) : (TResult)RawValue;
        }

        public abstract bool Equals(ConstantValue other);

        public static ConstantValue operator ==(ConstantValue a, ConstantValue b)
        {
            if (ReferenceEquals(a, b))
            {
                return True;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return False;
            }

            return Create(a.Equals(b));
        }

        public static ConstantValue operator !=(ConstantValue a, ConstantValue b)
        {
            if (ReferenceEquals(a, b))
            {
                return False;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return True;
            }

            return Create(!a.Equals(b));
        }

        public static ConstantValue operator <(ConstantValue a, ConstantValue b)
        {
            ThrowIfNullReference(a, b);
            return Create(a.ConvertTo(NsBuiltInType.Integer).IntegerValue < b.ConvertTo(NsBuiltInType.Integer).IntegerValue);
        }

        public static ConstantValue operator <=(ConstantValue a, ConstantValue b)
        {
            ThrowIfNullReference(a, b);
            return Create(a.ConvertTo(NsBuiltInType.Integer).IntegerValue <= b.ConvertTo(NsBuiltInType.Integer).IntegerValue);
        }

        public static ConstantValue operator >(ConstantValue a, ConstantValue b)
        {
            ThrowIfNullReference(a, b);
            return Create(a.ConvertTo(NsBuiltInType.Integer).IntegerValue > b.ConvertTo(NsBuiltInType.Integer).IntegerValue);
        }

        public static ConstantValue operator >=(ConstantValue a, ConstantValue b)
        {
            ThrowIfNullReference(a, b);
            return Create(a.ConvertTo(NsBuiltInType.Integer).IntegerValue >= b.ConvertTo(NsBuiltInType.Integer).IntegerValue);
        }

        public static ConstantValue operator +(ConstantValue a, ConstantValue b)
        {
            ThrowIfNullReference(a, b);
            return a.Add(b);
        }

        public static ConstantValue operator -(ConstantValue a, ConstantValue b)
        {
            ThrowIfNullReference(a, b);

            a = a.ConvertTo(NsBuiltInType.Integer);
            b = b.ConvertTo(NsBuiltInType.Integer);
            int value = a.IntegerValue - b.IntegerValue;
            bool isDelta = a.IsDeltaIntegerValue || b.IsDeltaIntegerValue;
            return new ConstantValueInteger(value, isDelta);
        }

        public static ConstantValue operator *(ConstantValue a, ConstantValue b)
        {
            ThrowIfNullReference(a, b);

            a = a.ConvertTo(NsBuiltInType.Integer);
            b = b.ConvertTo(NsBuiltInType.Integer);
            int value = a.IntegerValue * b.IntegerValue;
            bool isDelta = a.IsDeltaIntegerValue || b.IsDeltaIntegerValue;
            return new ConstantValueInteger(value, isDelta);
        }

        public static ConstantValue operator /(ConstantValue a, ConstantValue b)
        {
            ThrowIfNullReference(a, b);

            a = a.ConvertTo(NsBuiltInType.Integer);
            b = b.ConvertTo(NsBuiltInType.Integer);
            int value = a.IntegerValue / b.IntegerValue;
            bool isDelta = a.IsDeltaIntegerValue || b.IsDeltaIntegerValue;
            return new ConstantValueInteger(value, isDelta);
        }

        public static ConstantValue operator !(ConstantValue value)
        {
            ThrowIfNullReference(value);
            if (value.Type == NsBuiltInType.String)
            {
                ThrowInvalidUnary("!", value);
            }

            return Create(!Convert.ToBoolean(value.RawValue));
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
            return new ConstantValueInteger(-value.IntegerValue, value.IsDeltaIntegerValue);
        }

        public static ConstantValue operator++(ConstantValue value)
        {
            ThrowIfNullReference(value);
            value = value.ConvertTo(NsBuiltInType.Integer);
            return new ConstantValueInteger(value.IntegerValue + 1, value.IsDeltaIntegerValue);
        }

        public static ConstantValue operator --(ConstantValue value)
        {
            ThrowIfNullReference(value);
            value = value.ConvertTo(NsBuiltInType.Integer);
            return new ConstantValueInteger(value.IntegerValue - 1, value.IsDeltaIntegerValue);
        }

        public static bool operator true(ConstantValue value)
        {
            ThrowIfNullReference(value);
            switch (value.Type)
            {
                case NsBuiltInType.Boolean:
                    return (bool)value.RawValue;

                case NsBuiltInType.Integer:
                    return (int)value.RawValue > 0;

                case NsBuiltInType.String:
                default:
                    ThrowInvalidUnary("true", value);
                    return false;
            }
        }

        public static bool operator false(ConstantValue value)
        {
            ThrowIfNullReference(value);
            switch (value.Type)
            {
                case NsBuiltInType.Boolean:
                    return !(bool)value.RawValue;

                case NsBuiltInType.Integer:
                    return (int)value.RawValue == 0;

                case NsBuiltInType.String:
                default:
                    ThrowInvalidUnary("false", value);
                    return false;
            }
        }

        public static ConstantValue operator |(ConstantValue a, ConstantValue b)
        {
            ThrowIfNullReference(a, b);
            if (a.Type != NsBuiltInType.Boolean || b.Type != NsBuiltInType.Boolean)
            {
                ThrowInvalidBinary("|", a, b);
            }

            return Create((bool)a.RawValue | (bool)b.RawValue);
        }

        public static ConstantValue operator &(ConstantValue a, ConstantValue b)
        {
            ThrowIfNullReference(a, b);
            if (a.Type != NsBuiltInType.Boolean || b.Type != NsBuiltInType.Boolean)
            {
                ThrowInvalidBinary("&", a, b);
            }

            return Create((bool)a.RawValue & (bool)b.RawValue);
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
                throw new InvalidOperationException("ConstantValue.Null should be used instead of 'null'.");
            }
        }

        private static void ThrowIfNullReference(ConstantValue a, ConstantValue b)
        {
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                throw new InvalidOperationException("ConstantValue.Null should be used instead of 'null'.");
            }
        }

        public static void ThrowInvalidUnary(string op, ConstantValue value)
        {
            string errorMessage = $"Operator '{op}' cannot be applied to an operand of type '{value.Type}'.";
            throw new InvalidOperationException(errorMessage);
        }

        private static void ThrowInvalidBinary(string op, ConstantValue a, ConstantValue b)
        {
            string errorMessage = $"Operator '{op}' cannot be applied to operands of type '{a.Type}' and '{b.Type}'.";
            throw new InvalidOperationException(errorMessage);
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

            public override bool Equals(ConstantValue other)
            {
                if (ReferenceEquals(other, null))
                {
                    return false;
                }
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                switch (other)
                {
                    case ConstantValueInteger i:
                        return IntegerValue == i.IntegerValue && IsDeltaIntegerValue == i.IsDeltaIntegerValue;

                    case ConstantValueBoolean b:
                        // Ints greater than 1 are neither equal to true nor equal to false.
                        return IntegerValue == 0 && b.BooleanValue == false || IntegerValue == 1 && b.BooleanValue == true;

                    case ConstantValueString _:
                        // An integer and a string can never be equal.
                        return false;

                    case ConstantValueNull @null:
                        return @null.Equals(this);

                    default:
                        throw EqualityNotDefined(Type, other.Type);
                }
            }

            public override ConstantValue ConvertTo(NsBuiltInType targetType)
            {
                switch (targetType)
                {
                    case NsBuiltInType.Integer:
                        return this;

                    case NsBuiltInType.String:
                        return ConstantValue.Create(IntegerValue.ToString());

                    case NsBuiltInType.Boolean:
                        Debug.Assert(IntegerValue == 0 || IntegerValue == 1);
                        return ConstantValue.Create(IntegerValue == 1);

                    default:
                        throw InvalidConversion(Type, targetType);
                }
            }

            public override ConstantValue Add(ConstantValue valueToAdd)
            {
                switch (valueToAdd)
                {
                    case ConstantValueInteger i:
                        bool isDelta = IsDeltaIntegerValue || i.IsDeltaIntegerValue;
                        return ConstantValue.Create(IntegerValue + i.IntegerValue, isDelta);

                    case ConstantValueString s:
                        if (RepresentsNumber(s.StringValue) || ReferenceEquals(s, EmptyString))
                        {
                            return Add(s.ConvertTo(NsBuiltInType.Integer));
                        }

                        return ConvertTo(NsBuiltInType.String).Add(s);

                    default:
                        return Add(valueToAdd.ConvertTo(NsBuiltInType.Integer));
                }

                bool RepresentsNumber(string s) => int.TryParse(s, out _);
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

            public override bool Equals(ConstantValue other)
            {
                if (ReferenceEquals(other, null))
                {
                    return false;
                }
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                switch (other)
                {
                    case ConstantValueBoolean b:
                        return BooleanValue == b.BooleanValue;

                    case ConstantValueInteger i:
                        return i.Equals(this);

                    case ConstantValueString s:
                        return false;

                    case ConstantValueNull @null:
                        return @null.Equals(this);

                    default:
                        throw EqualityNotDefined(Type, other.Type);
                }
            }

            public override ConstantValue ConvertTo(NsBuiltInType targetType)
            {
                throw new NotImplementedException();
            }

            public override ConstantValue Add(ConstantValue valueToAdd)
            {
                return ConvertTo(NsBuiltInType.Integer).Add(valueToAdd.ConvertTo(NsBuiltInType.Integer));
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

            public override bool Equals(ConstantValue other)
            {
                if (ReferenceEquals(other, null))
                {
                    return false;
                }
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                switch (other)
                {
                    case ConstantValueString s:
                        return StringValue == s.StringValue;

                    case ConstantValueInteger _:
                    case ConstantValueBoolean _:
                        return false;

                    case ConstantValueNull @null:
                        return @null.Equals(this);

                    default:
                        throw EqualityNotDefined(Type, other.Type);
                }
            }

            public override ConstantValue ConvertTo(NsBuiltInType targetType)
            {
                switch (targetType)
                {
                    case NsBuiltInType.String:
                        return this;

                    case NsBuiltInType.Integer:
                        return StringValue == "@" ? DeltaZero : Zero;

                    default:
                        throw InvalidConversion(Type, targetType);
                }
            }

            public override ConstantValue Add(ConstantValue valueToAdd)
            {
                if (StringValue == "@")
                {
                    return ConvertTo(NsBuiltInType.Integer).Add(valueToAdd);
                }

                valueToAdd = valueToAdd.ConvertTo(NsBuiltInType.String);
                return ConstantValue.Create(StringValue + valueToAdd.StringValue);
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

            public override ConstantValue Add(ConstantValue valueToAdd)
            {
                return valueToAdd.Add(this);
            }

            public override bool Equals(ConstantValue other)
            {
                if (other is ConstantValueString s)
                {
                    return s.StringValue.Equals("null", StringComparison.OrdinalIgnoreCase);
                }

                return ReferenceEquals(other, Default(other.Type));
            }
        }
    }
}
