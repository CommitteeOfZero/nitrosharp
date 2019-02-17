using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NitroSharp.Utilities;

namespace NitroSharp.NsScript
{
    public enum BuiltInType : byte
    {
        Uninitialized = 0,

        Null,
        Integer,
        DeltaInteger,
        Float,
        Boolean,
        String,
        BuiltInConstant
    }

    public readonly struct ConstantValue : IEquatable<ConstantValue>
    {
        private readonly int _numericValue;
        private readonly string? _stringValue;

        public readonly BuiltInType Type;

        public static ConstantValue Null => new ConstantValue(BuiltInType.Null);
        public static ConstantValue True => new ConstantValue(true);
        public static ConstantValue False => new ConstantValue(false);
        public static ConstantValue EmptyString => new ConstantValue(string.Empty);

        public static ConstantValue Integer(int value) => new ConstantValue(value, false);
        public static ConstantValue Delta(int delta) => new ConstantValue(delta, true);
        public static ConstantValue Float(float value) => new ConstantValue(value);
        public static ConstantValue String(string value) => new ConstantValue(value);
        public static ConstantValue Boolean(bool value) => value ? True : False;

        public static ConstantValue BuiltInConstant(BuiltInConstant value)
            => new ConstantValue(value);

        private ConstantValue(BuiltInType type)
        {
            Debug.Assert(type == BuiltInType.Null);
            _numericValue = 0;
            _stringValue = null;
            Type = type;
        }

        private ConstantValue(int value, bool isDelta)
        {
            _numericValue = value;
            _stringValue = null;
            Type = isDelta
                ? BuiltInType.DeltaInteger
                : BuiltInType.Integer;
        }

        private ConstantValue(float value)
        {
            _numericValue = Unsafe.As<float, int>(ref value);
            _stringValue = null;
            Type = BuiltInType.Integer;
        }

        private ConstantValue(bool value)
        {
            _numericValue = value ? 1 : 0;
            _stringValue = null;
            Type = BuiltInType.Boolean;
        }

        private ConstantValue(string value)
        {
            _stringValue = value;
            _numericValue = 0;
            Type = BuiltInType.String;
        }

        private ConstantValue(BuiltInConstant value)
        {
            _numericValue = (int)value;
            _stringValue = null;
            Type = BuiltInType.BuiltInConstant;
        }

        public bool IsNull => Type == BuiltInType.Null;
        public bool IsString => Type == BuiltInType.String;
        public bool IsZero => Type == BuiltInType.Integer && _numericValue == 0;
        public bool IsEmptyString => Type == BuiltInType.String && _stringValue == string.Empty;

        public bool IsAtCharacter =>
            _stringValue != null && _stringValue == "@";

        public int? AsDelta()
            => Type == BuiltInType.DeltaInteger
                ? _numericValue
                : (int?)null;

        public int? AsInteger()
        {
            switch (Type)
            {
                case BuiltInType.Integer:
                case BuiltInType.Boolean:
                case BuiltInType.BuiltInConstant:
                    return _numericValue;
                case BuiltInType.String:
                    return _stringValue == string.Empty
                        ? 0 : (int?)null;
                default:
                    return null;
            }
        }

        public float? AsFloat()
        {
            if (Type == BuiltInType.Float)
            {
                int value = _numericValue;
                return Unsafe.As<int, float>(ref value);
            }

            return null;
        }

        public bool? AsBool()
            => Type == BuiltInType.Boolean || Type == BuiltInType.Integer
                ? _numericValue > 0 : (bool?)null;

        public string? AsString()
            => Type == BuiltInType.String ? _stringValue : null;

        public BuiltInConstant? AsBuiltInConstant()
            => Type == BuiltInType.BuiltInConstant
                ? (BuiltInConstant)_numericValue
                : (BuiltInConstant?)null;

        private static bool Equals(ConstantValue left, ConstantValue right)
        {
            bool equal;
            (int? leftNum, int? rightNum) = (left.AsInteger(), right.AsInteger());
            if (leftNum.HasValue && rightNum.HasValue)
            {
                equal = leftNum.Value == rightNum.Value;
            }
            else if (left.IsString && right.IsString)
            {
                equal = left._stringValue!.Equals(right._stringValue!);
            }
            else
            {
                equal = left.IsNull && right.IsNull;
            }

            return equal;
        }

        public string ConvertToString() => Type switch
        {
            BuiltInType.String => _stringValue!,
            BuiltInType.Boolean => _numericValue > 0 ? "true" : "false",
            BuiltInType.Integer => _numericValue.ToString(),
            BuiltInType.DeltaInteger => "@" + _numericValue.ToString(),
            BuiltInType.Float => AsFloat()!.Value!.ToString(),
            BuiltInType.BuiltInConstant => ((BuiltInConstant)_numericValue).ToString(),
            BuiltInType.Null => "null",
            _ => ThrowHelper.Unreachable<string>()
        };

        public static ConstantValue operator ==(in ConstantValue left, in ConstantValue right)
            => Boolean(Equals(left, right));

        public static ConstantValue operator !=(in ConstantValue left, in ConstantValue right)
            => Boolean(!Equals(left, right));

        public static ConstantValue operator +(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Integer, BuiltInType.Integer)
                    => Integer(left.AsInteger()!.Value + right.AsInteger()!.Value),
                (BuiltInType.Integer, BuiltInType.String)
                    => integerString(left, right),
                (BuiltInType.String, BuiltInType.Integer)
                    => integerString(left, right),
                (BuiltInType.Float, BuiltInType.Float)
                    => Float(left.AsFloat()!.Value + right.AsFloat()!.Value),
                _   => String(left.ConvertToString() + right.ConvertToString())
            };

            static ConstantValue integerString(in ConstantValue left, in ConstantValue right)
            {
                return left.IsAtCharacter
                    ? Delta(right._numericValue)
                    : String(left.ConvertToString() + right.ConvertToString());
            }
        }

        public static ConstantValue operator -(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Integer, BuiltInType.Integer)
                    => Integer(left.AsInteger()!.Value - right.AsInteger()!.Value),
                (BuiltInType.Float, BuiltInType.Float)
                    => Float(left.AsFloat()!.Value - right.AsFloat()!.Value),
                _   => InvalidBinOp("-", left.Type, right.Type)
            };
        }

        public static ConstantValue operator *(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Integer, BuiltInType.Integer)
                    => Integer(left.AsInteger()!.Value * right.AsInteger()!.Value),
                (BuiltInType.Float, BuiltInType.Float)
                    => Float(left.AsFloat()!.Value * right.AsFloat()!.Value),
                _   => InvalidBinOp("*", left.Type, right.Type)
            };
        }

        public static ConstantValue operator /(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Integer, BuiltInType.Integer)
                    => Integer(left.AsInteger()!.Value / right.AsInteger()!.Value),
                (BuiltInType.Float, BuiltInType.Float)
                    => Float(left.AsFloat()!.Value / right.AsFloat()!.Value),
                _   => InvalidBinOp("/", left.Type, right.Type)
            };
        }

        public static ConstantValue operator<(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Integer, BuiltInType.Integer)
                    => Boolean(left.AsInteger()!.Value < right.AsInteger()!.Value),
                (BuiltInType.Float, BuiltInType.Float)
                    => Boolean(left.AsFloat()!.Value < right.AsFloat()!.Value),
                _   => InvalidBinOp("<", left.Type, right.Type)
            };
        }

        public static ConstantValue operator <=(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Integer, BuiltInType.Integer)
                    => Boolean(left.AsInteger()!.Value <= right.AsInteger()!.Value),
                (BuiltInType.Float, BuiltInType.Float)
                    => Boolean(left.AsFloat()!.Value <= right.AsFloat()!.Value),
                _   => InvalidBinOp("<=", left.Type, right.Type)
            };
        }

        public static ConstantValue operator >(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Integer, BuiltInType.Integer)
                    => Boolean(left.AsInteger()!.Value > right.AsInteger()!.Value),
                (BuiltInType.Float, BuiltInType.Float)
                    => Boolean(left.AsFloat()!.Value > right.AsFloat()!.Value),
                _   => InvalidBinOp(">", left.Type, right.Type)
            };
        }

        public static ConstantValue operator >=(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Integer, BuiltInType.Integer)
                    => Boolean(left.AsInteger()!.Value >= right.AsInteger()!.Value),
                (BuiltInType.Float, BuiltInType.Float)
                    => Boolean(left.AsFloat()!.Value >= right.AsFloat()!.Value),
                _   => InvalidBinOp(">=", left.Type, right.Type)
            };
        }

        public static ConstantValue operator ++(in ConstantValue value)
        {
            return value.Type switch
            {
                BuiltInType.Integer => Integer(value._numericValue + 1),
                BuiltInType.Float => Float(value.AsFloat()!.Value + 1),
                _ => InvalidOp("++", value.Type)
            };
        }

        public static ConstantValue operator --(ConstantValue value)
        {
            return value.Type switch
            {
                BuiltInType.Integer => Integer(value._numericValue - 1),
                BuiltInType.Float => Float(value.AsFloat()!.Value - 1),
                _ => InvalidOp("++", value.Type)
            };
        }

        public static ConstantValue operator &(in ConstantValue left, in ConstantValue right)
        {
            (bool? leftBool, bool? rightBool) = (left.AsBool(), right.AsBool());
            if (leftBool.HasValue && rightBool.HasValue)
            {
                return Boolean(leftBool.Value && rightBool.Value);
            }

            return InvalidBinOp("&&", left.Type, right.Type);
        }

        public static ConstantValue operator |(in ConstantValue left, in ConstantValue right)
        {
            (bool? leftBool, bool? rightBool) = (left.AsBool(), right.AsBool());
            if (leftBool.HasValue && rightBool.HasValue)
            {
                return Boolean(leftBool.Value || rightBool.Value);
            }

            return InvalidBinOp("||", left.Type, right.Type);
        }

        public static bool operator true(in ConstantValue value)
            => value.AsBool() ?? false;

        public static bool operator false(ConstantValue value)
            => !(value.AsBool() ?? false);

        public override string ToString() => ConvertToString();
        public bool Equals(ConstantValue other) => Equals(this, other);

        public override bool Equals(object obj)
            => obj is ConstantValue other && Equals(this, other);

        public override int GetHashCode()
        {
            return Type != BuiltInType.String
                ? HashHelper.Combine((int)Type, _numericValue)
                : HashHelper.Combine((int)Type, _stringValue!.GetHashCode());
        }

        private static ConstantValue InvalidOp(string op, BuiltInType type)
            => throw new InvalidOperationException($"Operator '{op}' cannot be applied to operand of type '{type.ToString()}'.");

        private static ConstantValue InvalidBinOp(string op, BuiltInType leftType, BuiltInType rightType)
            => throw new InvalidOperationException($"Operator '{op}' cannot be applied to operands of type '{leftType.ToString()}' and '{rightType.ToString()}'.");
    }
}
