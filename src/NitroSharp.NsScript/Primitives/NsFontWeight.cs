using System.Runtime.InteropServices;

namespace NitroSharp.NsScript.Primitives;

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

    private NsFontWeight(StandardFontWeight value) : this()
    {
        Variant = NsFontWeightVariant.Standard;
        Standard = value;
    }

    private NsFontWeight(int value) : this()
    {
        Variant = NsFontWeightVariant.Custom;
        Custom = value;
    }

    public static NsFontWeight From(in ConstantValue value)
    {
        return value.Type switch
        {
            BuiltInType.Numeric => new NsFontWeight((int)value.AsNumber()!.Value),
            BuiltInType.BuiltInConstant => new NsFontWeight(mapConstant(value.AsBuiltInConstant()!.Value)),
            _ => throw ThrowHelper.ArgumentInvalid(nameof(value))
        };

        static StandardFontWeight mapConstant(BuiltInConstant val)
        {
            return val switch
            {
                BuiltInConstant.Normal => StandardFontWeight.Normal,
                BuiltInConstant.Medium => StandardFontWeight.Medium,
                BuiltInConstant.Heavy => StandardFontWeight.Bold,
                _ => throw ThrowHelper.ArgumentOutOfRange(nameof(val))
            };
        }
    }
}
