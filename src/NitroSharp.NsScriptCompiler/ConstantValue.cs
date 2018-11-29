using System;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScriptNew
{
    public enum BuiltInType : byte
    {
        Null,
        Integer,
        Float,
        Boolean,
        String,
        EnumValue
    }

    public readonly struct ConstantValue
    {
        private readonly int _numericValue;
        private readonly string _stringValue;

        public readonly BuiltInType Type;

        public static ConstantValue Null => default;
        public static ConstantValue True => new ConstantValue(true);
        public static ConstantValue False => new ConstantValue(false);
        public static ConstantValue EmptyString => new ConstantValue(string.Empty);

        public static ConstantValue Integer(int value) => new ConstantValue(value);
        public static ConstantValue Float(float value) => new ConstantValue(value);
        public static ConstantValue String(string value) => new ConstantValue(value);

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

        public bool IsNull => Type == BuiltInType.Null;

        public int IntegerValue
        {
            get
            {
                if (Type != BuiltInType.Integer)
                {
                    ThrowNotAnInteger();
                }

                return _numericValue;
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

                return _stringValue;
            }
        }

        private void ThrowNotAnInteger()
            => throw new InvalidOperationException("Value is not an integer.");

        private void ThrowNotFloat()
            => throw new InvalidOperationException("Value is not a float.");

        private void ThrowNotString()
            => throw new InvalidOperationException("Value is not a string.");
    }
}
