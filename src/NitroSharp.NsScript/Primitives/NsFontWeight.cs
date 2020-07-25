using System.Runtime.InteropServices;

namespace NitroSharp.NsScript.Primitives
{
    public enum StandardFontWeight
    {
        Normal,
        Medium,
        Bold
    }

    public enum NsFontWeightVariant
    {
        Standard,
        Custom
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct NsFontWeight
    {
        [FieldOffset(0)]
        public readonly NsFontWeightVariant Variant;

        [FieldOffset(4)]
        public readonly StandardFontWeight Standard;

        [FieldOffset(4)]
        public readonly int Custom;

        public NsFontWeight(StandardFontWeight value) : this()
            => (Variant, Standard) = (NsFontWeightVariant.Standard, value);

        public NsFontWeight(int value) : this()
            => (Variant, Custom) = (NsFontWeightVariant.Custom, value);

        public static NsFontWeight From(in ConstantValue value)
        {
            static StandardFontWeight mapConstant(BuiltInConstant val) => val switch
            {
                BuiltInConstant.Normal => StandardFontWeight.Normal,
                BuiltInConstant.Medium => StandardFontWeight.Medium,
                BuiltInConstant.Heavy => StandardFontWeight.Bold,
                _ => ThrowHelper.UnexpectedValue<StandardFontWeight>()
            };

            return value.Type switch
            {
                BuiltInType.Numeric => new NsFontWeight((int)value.AsNumber()!.Value),
                BuiltInType.BuiltInConstant => new NsFontWeight(mapConstant(value.AsBuiltInConstant()!.Value)),
                _ => ThrowHelper.UnexpectedValue<NsFontWeight>()
            };
        }
    }
}
