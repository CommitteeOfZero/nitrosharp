using System.Runtime.InteropServices;

namespace NitroSharp.NsScriptNew.VM
{
    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct Immediate
    {
        [FieldOffset(0)]
        public readonly BuiltInType Type;

        [FieldOffset(4)]
        public readonly int IntegerValue;
        [FieldOffset(4)]
        public readonly float FloatValue;
        [FieldOffset(4)]
        public readonly ushort StringToken;
        [FieldOffset(4)]
        public readonly BuiltInConstant Constant;

        internal Immediate(int integerValue) : this()
            => (Type, IntegerValue) = (BuiltInType.Integer, integerValue);

        internal Immediate(float floatValue) : this()
            => (Type, FloatValue) = (BuiltInType.Float, floatValue);

        internal Immediate(ushort stringToken) : this()
            => (Type, StringToken) = (BuiltInType.String, stringToken);

        internal Immediate(BuiltInConstant constant) : this()
            => (Type, Constant) = (BuiltInType.BuiltInConstant, constant);
    }

}
