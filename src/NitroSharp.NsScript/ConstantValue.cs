using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MessagePack;
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

        public static ConstantValue Null => new(BuiltInType.Null);
        public static ConstantValue True => new(true);
        public static ConstantValue False => new(false);
        public static ConstantValue EmptyString => new(string.Empty);

        public static ConstantValue Number(float value) => new(value, false);
        public static ConstantValue Delta(float delta) => new(delta, true);
        public static ConstantValue String(string value) => new(value);
        public static ConstantValue Boolean(bool value) => value ? True : False;

        public static ConstantValue BezierCurve(CompositeBezier bezierCurve)
            => new(bezierCurve);

        public static ConstantValue BuiltInConstant(BuiltInConstant value)
            => new(value);

        public ConstantValue WithSlot(short slot) => new(this, slot);

        public ConstantValue(ref MessagePackReader reader) : this()
        {
            reader.ReadArrayHeader();
            Type = (BuiltInType)reader.ReadInt32();
            switch (Type)
            {
                case BuiltInType.Numeric:
                case BuiltInType.DeltaNumeric:
                case BuiltInType.Boolean:
                case BuiltInType.BuiltInConstant:
                    _numericValue = reader.ReadInt32();
                    break;
                case BuiltInType.String:
                    _stringValue = reader.ReadString();
                    break;
                case BuiltInType.BezierCurve:
                    _bezierCurve = new CompositeBezier(ref reader);
                    break;
            }
        }

        public void Serialize(ref MessagePackWriter writer)
        {
            int size = Type is BuiltInType.Null or BuiltInType.Uninitialized ? 1 : 2;
            writer.WriteArrayHeader(size);
            writer.Write((int)Type);
            switch (Type)
            {
                case BuiltInType.Numeric:
                case BuiltInType.DeltaNumeric:
                case BuiltInType.Boolean:
                case BuiltInType.BuiltInConstant:
                    writer.Write(_numericValue);
                    break;
                case BuiltInType.String:
                    writer.Write(_stringValue);
                    break;
                case BuiltInType.BezierCurve:
                    _bezierCurve.Serialize(ref writer);
                    break;
            }
        }

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
        public bool IsEmptyString => Type == BuiltInType.String && _stringValue!.Length == 0;

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
                : null;

        public float? AsNumber()
        {
            return Type switch
            {
                BuiltInType.Numeric => FloatValue,
                BuiltInType.Boolean or BuiltInType.BuiltInConstant => _numericValue,
                BuiltInType.String => _stringValue == string.Empty ? 0 : null,
                _ => null,
            };
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
                : null;

        public CompositeBezier? AsBezierCurve()
            => Type == BuiltInType.BezierCurve
                ? _bezierCurve
                : null;

        private static bool Equals(ConstantValue left, ConstantValue right)
        {
            (float? leftNum, float? rightNum) = (left.AsNumber(), right.AsNumber());
            if (leftNum.HasValue && rightNum.HasValue)
            {
                return leftNum.Value == rightNum.Value;
            }

            return left.IsString && right.IsString
                ? left._stringValue!.Equals(right._stringValue!)
                : left.IsNull && right.IsNull;
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

        public static ConstantValue operator %(in ConstantValue left, in ConstantValue right)
        {
            return (left.Type, right.Type) switch
            {
                (BuiltInType.Numeric, BuiltInType.Numeric)
                    => Number(left.AsNumber()!.Value % right.AsNumber()!.Value),
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
            => throw new InvalidOperationException($"Operator '{op}' cannot be applied to operand of type '{type}'.");

        private static ConstantValue InvalidBinOp(string op, BuiltInType leftType, BuiltInType rightType)
            => throw new InvalidOperationException($"Operator '{op}' cannot be applied to operands of type '{leftType}' and '{rightType}'.");
    }
}
