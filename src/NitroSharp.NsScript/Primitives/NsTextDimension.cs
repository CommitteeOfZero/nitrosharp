namespace NitroSharp.NsScript.Primitives;

public enum NsTextDimensionVariant
{
    Auto,
    Value
}

public readonly record struct NsTextDimension
{
    public readonly NsTextDimensionVariant Variant;
    public readonly int? Value;

    private NsTextDimension(NsTextDimensionVariant variant, int? value)
    {
        Variant = variant;
        Value = value;
    }

    public static NsTextDimension Auto
        => new(NsTextDimensionVariant.Auto, null);

    public static NsTextDimension WithValue(int value)
        => new(NsTextDimensionVariant.Value, value);

    public static NsTextDimension FromConstant(BuiltInConstant constant)
    {
        // TODO: Is Auto the only acceptable constant?
        return constant switch
        {
            BuiltInConstant.Auto => Auto,
            _ => throw ThrowHelper.ArgumentOutOfRange(nameof(constant))
        };
    }
}
