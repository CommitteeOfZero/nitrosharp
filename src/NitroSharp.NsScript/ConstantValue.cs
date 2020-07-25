using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NitroSharp.NsScript.Primitives;

namespace NitroSharp.NsScript
{
    public enum BuiltInType : byte
    {
        Uninitialized = 0,

        Null,
        Numeric,
        DeltaNumeric,
        Boolean,
        String,
        BuiltInConstant,
        BezierCurve
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct ConstantValue : IEquatable<ConstantValue>
    {
        private const short UnspecifiedSlot = -1;

        [FieldOffset(0)]
        private readonly string? _stringValue;

        [FieldOffset(0)]
        private readonly CompositeBezier _bezierCurve;

        [FieldOffset(8)]
        private readonly int _numericValue;

        [FieldOffset(12)]
        public readonly BuiltInType Type;

        [FieldOffset(14)]
        private readonly short _slot;

        private float FloatValue
        {
            get
            {
                int val = _numericValue;
                return Unsafe.As<int, float>(ref val);
            }
        }

        public static ConstantValue Null => new ConstantValue(BuiltInType.Null);
        public static ConstantValue True => new ConstantValue(true);
        public static ConstantValue False => new ConstantValue(false);
        public static ConstantValue EmptyString => new ConstantValue(string.Empty);

        public static ConstantValue Number(float value) => new ConstantValue(value, false);
        public static ConstantValue Delta(float delta) => new ConstantValue(delta, true);
        public static ConstantValue String(string value) => new ConstantValue(value);
        public static ConstantValue Boolean(bool value) => value ? True : False;

        public static ConstantValue BezierCurve(CompositeBezier bezierCurve)
            => new ConstantValue(bezierCurve);

        public static ConstantValue BuiltInConstant(BuiltInConstant value)
            => new ConstantValue(value);

        public ConstantValue WithSlot(short slot) => new ConstantValue(this, slot);

        private ConstantValue(in ConstantValue value, short slot)
        {
            this = value;
            _slot = slot;
        }

        private ConstantValue(CompositeBezier bezierCurve) : this()
        {
            Type = BuiltInType.BezierCurve;
            _bezierCurve = bezierCurve;
            _slot = UnspecifiedSlot;
        }

        private ConstantValue(BuiltInType type) : this()
        {
            Debug.Assert(type == BuiltInType.Null);
            _numericValue = 0;
            _stringValue = null;
            Type = type;
            _slot = UnspecifiedSlot;
        }

        private ConstantValue(float value, bool isDelta) : this()
        {
            _numericValue = Unsafe.As<float, int>(ref value);
            _stringValue = null;
            Type = isDelta
                ? BuiltInType.DeltaNumeric
                : BuiltInType.Numeric;
            _slot = UnspecifiedSlot;
        }

        private ConstantValue(float value) : this()
        {
            _numericValue = Unsafe.As<float, int>(ref value);
            _stringValue = null;
            Type = BuiltInType.Numeric;
            _slot = UnspecifiedSlot;
        }

        private ConstantValue(bool value) : this()
        {
            _numericValue = value ? 1 : 0;
            _stringValue = null;
            Type = BuiltInType.Boolean;
            _slot = UnspecifiedSlot;
        }

        private ConstantValue(string value) : this()
        {
            _stringValue = value;
            _numericValue = 0;
            Type = BuiltInType.String;
            _slot = UnspecifiedSlot;
        }

        private ConstantValue(BuiltInConstant value) : this()
        {
            _numericValue = (int)value;
            _stringValue = null;
            Type = BuiltInType.BuiltInConstant;
            _slot = UnspecifiedSlot;
        }

        public bool IsNull => Type == BuiltInType.Null;
        public bool IsString => Type == BuiltInType.String;
        public bool IsZero => Type == BuiltInType.Numeric && FloatValue == 0;
        public bool IsEmptyString => Type == BuiltInType.String && _stringValue == string.Empty;

        public bool GetSlotInfo(out short slot)
        {
            slot = _slot;
            return slot != UnspecifiedSlot;
        }

        public bool IsAtCharacter =>
            _stringValue != null && _stringValue == "@";

        public float? AsDeltaNumber()
            => Type == BuiltInType.DeltaNumeric
                ? FloatValue
                : (float?)null;

        public float? AsNumber()
        {
            switch (Type)
            {
                case BuiltInType.Numeric:
                    return FloatValue;
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

        public bool? AsBool() => Type switch
        {
            BuiltInType.Boolean => _numericValue > 0,
            BuiltInType.Numeric => FloatValue > 0,
            _ => null
        };

        public string? AsString()
            => Type == BuiltInType.String ? _stringValue : null;

        public BuiltInConstant? AsBuiltInConstant()
            => Type == BuiltInType.BuiltInConstant
                ? (BuiltInConstant)_numericValue
                : (BuiltInConstant?)null;

        public CompositeBezier? AsBezierCurve()
            => Type == BuiltInType.BezierCurve
                ? _bezierCurve
                : (CompositeBezier?)null;

        private static bool Equals(ConstantValue left, ConstantValue right)
        {
            bool equal;
            (float? leftNum, float? rightNum) = (left.AsNumber(), right.AsNumber());
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
            BuiltInType.Numeric => FloatValue.ToString(),
            BuiltInType.DeltaNumeric => "@" + FloatValue.ToString(),
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
                (BuiltInType.Numeric, BuiltInType.Numeric)
                    => Number(left.AsNumber()!.Value + right.AsNumber()!.Value),
                (BuiltInType.Numeric, BuiltInType.String)
                    => integerString(left, right),
                (BuiltInType.String, BuiltInType.Numeric)
                    => integerString(left, right),

                _   => String(left.ConvertToString() + right.ConvertToString())
            };

            static ConstantValue integerString(in ConstantValue left, in ConstantValue right)
            {
                return left.IsAtCharacter
                    ? Delta(right.FloatValue)
                    : String(left.ConvertToString() + right.ConvertToString());
            }
        }

        public static ConstantValue operator -(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Numeric, BuiltInType.Numeric)
                    => Number(left.AsNumber()!.Value - right.AsNumber()!.Value),
                _   => InvalidBinOp("-", left.Type, right.Type)
            };
        }

        public static ConstantValue operator *(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Numeric, BuiltInType.Numeric)
                    => Number(left.AsNumber()!.Value * right.AsNumber()!.Value),
                _   => InvalidBinOp("*", left.Type, right.Type)
            };
        }

        public static ConstantValue operator /(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Numeric, BuiltInType.Numeric)
                    => Number(left.AsNumber()!.Value / right.AsNumber()!.Value),
                _   => InvalidBinOp("/", left.Type, right.Type)
            };
        }

        public static ConstantValue operator<(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Numeric, BuiltInType.Numeric)
                    => Boolean(left.AsNumber()!.Value < right.AsNumber()!.Value),
                _   => InvalidBinOp("<", left.Type, right.Type)
            };
        }

        public static ConstantValue operator <=(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Numeric, BuiltInType.Numeric)
                    => Boolean(left.AsNumber()!.Value <= right.AsNumber()!.Value),
                _   => InvalidBinOp("<=", left.Type, right.Type)
            };
        }

        public static ConstantValue operator >(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Numeric, BuiltInType.Numeric)
                    => Boolean(left.AsNumber()!.Value > right.AsNumber()!.Value),
                _   => InvalidBinOp(">", left.Type, right.Type)
            };
        }

        public static ConstantValue operator >=(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Numeric, BuiltInType.Numeric)
                    => Boolean(left.AsNumber()!.Value >= right.AsNumber()!.Value),
                _   => InvalidBinOp(">=", left.Type, right.Type)
            };
        }

        public static ConstantValue operator ++(in ConstantValue value)
        {
            return value.Type switch
            {
                BuiltInType.Numeric => Number(value.FloatValue + 1),
                _ => InvalidOp("++", value.Type)
            };
        }

        public static ConstantValue operator --(ConstantValue value)
        {
            return value.Type switch
            {
                BuiltInType.Numeric => Number(value.FloatValue - 1),
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

        public override string ToString()
            => !IsString ? ConvertToString() : $"\"{ConvertToString()}\"";

        public bool Equals(ConstantValue other) => Equals(this, other);

        public override bool Equals(object? obj)
            => obj is ConstantValue other && Equals(this, other);

        public override int GetHashCode()
        {
            return Type switch
            {
                BuiltInType.String => HashCode.Combine(_stringValue),
                BuiltInType.Null => 0,
                BuiltInType.Uninitialized => -1,
                BuiltInType.BezierCurve => _bezierCurve.GetHashCode(),
                _ => HashCode.Combine(Type, _numericValue)
            };
        }

        private static ConstantValue InvalidOp(string op, BuiltInType type)
            => throw new InvalidOperationException($"Operator '{op}' cannot be applied to operand of type '{type.ToString()}'.");

        private static ConstantValue InvalidBinOp(string op, BuiltInType leftType, BuiltInType rightType)
            => throw new InvalidOperationException($"Operator '{op}' cannot be applied to operands of type '{leftType.ToString()}' and '{rightType.ToString()}'.");
    }
}
