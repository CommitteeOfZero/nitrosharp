using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScriptNew
{
    public enum BuiltInType : byte
    {
        Null = 0,
        Integer,
        Float,
        Boolean,
        String,
        BuiltInConstant
    }

    public readonly struct ConstantValue
    {
        private readonly int _numericValue;
        private readonly string? _stringValue;

        public readonly BuiltInType Type;

        public static ConstantValue Null => default;
        public static ConstantValue True => new ConstantValue(true);
        public static ConstantValue False => new ConstantValue(false);
        public static ConstantValue EmptyString => new ConstantValue(string.Empty);

        public static ConstantValue Integer(int value) => new ConstantValue(value);
        public static ConstantValue Float(float value) => new ConstantValue(value);
        public static ConstantValue String(string value) => new ConstantValue(value);
        public static ConstantValue BuiltInConstant(BuiltInConstant value)
            => new ConstantValue(value);

        private ConstantValue(int value)
        {
            _numericValue = value;
            _stringValue = null;
            Type = BuiltInType.Integer;
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

        public BuiltInConstant BuiltInConstantValue
        {
            get
            {
                if (Type != BuiltInType.BuiltInConstant)
                {
                    ThrowNotBuiltInConstant();
                }

                return (BuiltInConstant)_numericValue;
            }
        }

        public int IntegerValue
        {
            get
            {
                if (Type != BuiltInType.Integer)
                {
                    ThrowNotInteger();
                }

                return _numericValue;
            }
        }

        public bool BooleanValue
        {
            get
            {
                if (Type != BuiltInType.Boolean)
                {
                    ThrowNotBoolean();
                }

                return (uint)_numericValue > 0u;
            }
        }

        public float FloatValue
        {
            get
            {
                if (Type != BuiltInType.Float)
                {
                    ThrowNotFloat();
                }

                int value = _numericValue;
                return Unsafe.As<int, float>(ref value);
            }
        }

        public string StringValue
        {
            get
            {
                if (Type != BuiltInType.String)
                {
                    ThrowNotString();
                }

                Debug.Assert(_stringValue != null);
                return _stringValue;
            }
        }

        private void ThrowNotInteger()
            => throw new InvalidOperationException("Value is not an integer.");

        private void ThrowNotBoolean()
            => throw new InvalidOperationException("Value is not a boolean.");

        private void ThrowNotFloat()
            => throw new InvalidOperationException("Value is not a float.");

        private void ThrowNotString()
            => throw new InvalidOperationException("Value is not a string.");

        private void ThrowNotBuiltInConstant()
            => throw new InvalidOperationException("Value is not a built-in constant.");
    }
}
