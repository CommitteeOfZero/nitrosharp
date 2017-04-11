using System;

namespace CommitteeOfZero.NsScript
{
    public sealed class ConstantValue : Expression, IEquatable<ConstantValue>
    {
        private static readonly ConstantValue s_true = new ConstantValue(true);
        private static readonly ConstantValue s_false = new ConstantValue(false);
        private static readonly ConstantValue s_zero = new ConstantValue(0);
        private static readonly ConstantValue s_null = new ConstantValue(null);

        public static ConstantValue True => s_true;
        public static ConstantValue False => s_false;
        public static ConstantValue Zero => s_zero;
        public static ConstantValue Null => s_null;

        public ConstantValue(object value, bool? isDelta = null)
        {
            if (value == null)
            {
                Type = NsType.Null;
                return;
            }

            if (value.GetType() == typeof(int))
            {
                Type = NsType.Integer;
                IsDelta = isDelta;
            }
            else if (value.GetType() == typeof(string))
            {
                Type = NsType.String;
            }
            else if (value.GetType() == typeof(bool))
            {
                Type = NsType.Boolean;
            }
            else
            {
                throw new ArgumentException();
            }

            RawValue = value;
        }

        public static ConstantValue Bool(bool value) => value ? True : False;

        public object RawValue { get; }
        public NsType Type { get; }
        public bool? IsDelta { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.ConstantValue;

        public TResult As<TResult>()
        {
            return Type == NsType.Null ? default(TResult) : (TResult)RawValue;
        }

        public static ConstantValue operator +(ConstantValue a, ConstantValue b)
        {
            if (a.Type == NsType.String)
            {
                if (a.As<string>() == "@" && b.Type == NsType.Integer)
                {
                    return new ConstantValue(b.RawValue, isDelta: true);
                }
            }

            if (a.Type != b.Type)
            {
                ThrowInvalidBinary("+", a, b);
            }

            if (a.Type == NsType.Boolean || b.Type == NsType.Boolean)
            {
                ThrowInvalidBinary("+", a, b);
            }

            if (a.Type == NsType.Integer)
            {
                return new ConstantValue((int)a.RawValue + (int)b.RawValue);
            }

            return new ConstantValue((string)a.RawValue + (string)b.RawValue);
        }

        public static ConstantValue operator -(ConstantValue a, ConstantValue b)
        {
            if (a.Type != NsType.Integer || b.Type != NsType.Integer)
            {
                ThrowInvalidBinary("-", a, b);
            }

            return new ConstantValue((int)a.RawValue - (int)b.RawValue);
        }

        public static ConstantValue operator *(ConstantValue a, ConstantValue b)
        {
            if (a.Type != NsType.Integer || b.Type != NsType.Integer)
            {
                ThrowInvalidBinary("*", a, b);
            }

            return new ConstantValue((int)a.RawValue * (int)b.RawValue);
        }

        public static ConstantValue operator /(ConstantValue a, ConstantValue b)
        {
            if (a.Type != NsType.Integer || b.Type != NsType.Integer)
            {
                ThrowInvalidBinary("/", a, b);
            }

            return new ConstantValue((int)a.RawValue / (int)b.RawValue);
        }

        public static ConstantValue operator ==(ConstantValue a, ConstantValue b)
        {
            return EqualsImpl(a, b);
        }

        public static ConstantValue operator !=(ConstantValue a, ConstantValue b)
        {
            return !EqualsImpl(a, b);
        }

        public static ConstantValue operator <(ConstantValue a, ConstantValue b)
        {
            if (a.Type != NsType.Integer || b.Type != NsType.Integer)
            {
                ThrowInvalidBinary("<", a, b);
            }

            return Bool((int)a.RawValue < (int)b.RawValue);
        }

        public static ConstantValue operator <=(ConstantValue a, ConstantValue b)
        {
            if (a.Type != NsType.Integer || b.Type != NsType.Integer)
            {
                ThrowInvalidBinary("<='", a, b);
            }

            return Bool((int)a.RawValue <= (int)b.RawValue);
        }

        public static ConstantValue operator >(ConstantValue a, ConstantValue b)
        {
            if (a.Type != NsType.Integer || b.Type != NsType.Integer)
            {
                ThrowInvalidBinary(">", a, b);
            }

            return Bool((int)a.RawValue > (int)b.RawValue);
        }

        public static ConstantValue operator >=(ConstantValue a, ConstantValue b)
        {
            if (a.Type != NsType.Integer || b.Type != NsType.Integer)
            {
                ThrowInvalidBinary(">=", a, b);
            }

            return Bool((int)a.RawValue >= (int)b.RawValue);
        }

        public static ConstantValue operator !(ConstantValue value)
        {
            if (value.Type == NsType.String)
            {
                ThrowInvalidUnary("!", value);
            }

            return Bool(!Convert.ToBoolean(value.RawValue));
        }

        public static ConstantValue operator +(ConstantValue value)
        {
            if (value.Type != NsType.Integer)
            {
                ThrowInvalidUnary("+", value);
            }

            return new ConstantValue(value.RawValue);
        }

        public static ConstantValue operator -(ConstantValue value)
        {
            if (value.Type != NsType.Integer)
            {
                ThrowInvalidUnary("-", value);
            }

            return new ConstantValue(-(int)value.RawValue, isDelta: value.IsDelta);
        }

        public static ConstantValue operator++(ConstantValue value)
        {
            if (value.Type != NsType.Integer)
            {
                ThrowInvalidUnary("++", value);
            }

            return new ConstantValue((int)value.RawValue + 1);
        }

        public static ConstantValue operator --(ConstantValue value)
        {
            if (value.Type != NsType.Integer)
            {
                ThrowInvalidUnary("--", value);
            }

            return new ConstantValue((int)value.RawValue - 1);
        }

        public static bool operator true(ConstantValue value)
        {
            switch (value.Type)
            {
                case NsType.Boolean:
                    return (bool)value.RawValue;

                case NsType.Integer:
                    return (int)value.RawValue > 0;

                case NsType.String:
                default:
                    ThrowInvalidUnary("true", value);
                    return false;
            }
        }

        public static bool operator false(ConstantValue value)
        {
            switch (value.Type)
            {
                case NsType.Boolean:
                    return (bool)value.RawValue;

                case NsType.Integer:
                    return (int)value.RawValue == 0;

                case NsType.String:
                default:
                    ThrowInvalidUnary("false", value);
                    return false;
            }
        }

        public static ConstantValue operator |(ConstantValue a, ConstantValue b)
        {
            if (a.Type != NsType.Boolean || b.Type != NsType.Boolean)
            {
                ThrowInvalidBinary("|", a, b);
            }

            return Bool((bool)a.RawValue | (bool)b.RawValue);
        }

        public static ConstantValue operator &(ConstantValue a, ConstantValue b)
        {
            if (a.Type != NsType.Boolean || b.Type != NsType.Boolean)
            {
                ThrowInvalidBinary("&", a, b);
            }

            return Bool((bool)a.RawValue & (bool)b.RawValue);
        }

        public override bool Equals(object obj)
        {
            return (bool)EqualsImpl(this, obj as ConstantValue).RawValue == true;
        }

        public override int GetHashCode()
        {
            return 17 * 29 + RawValue.GetHashCode();
        }

        private static ConstantValue EqualsImpl(ConstantValue a, ConstantValue b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return True;
            }

            if (object.ReferenceEquals(a, null) && object.ReferenceEquals(b, null))
            {
                return True;
            }

            if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null))
            {
                return False;
            }

            return Bool(object.Equals(a.RawValue, b.RawValue));
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

        public bool Equals(ConstantValue other)
        {
            return (bool)EqualsImpl(this, other).RawValue == true;
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
    }
}
