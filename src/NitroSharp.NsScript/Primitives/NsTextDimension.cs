namespace NitroSharp.NsScript.Primitives
{
    public enum NsTextDimensionVariant
    {
        Auto,
        Value
    }

    public readonly struct NsTextDimension
    {
        public readonly NsTextDimensionVariant Variant;
        public readonly int? Value;

        public NsTextDimension(NsTextDimensionVariant variant, int? value)
            => (Variant, Value) = (variant, value);

        public static NsTextDimension Auto
            => new(NsTextDimensionVariant.Auto, null);

        public static NsTextDimension WithValue(int value)
            => new(NsTextDimensionVariant.Value, value);

        public static NsTextDimension FromConstant(BuiltInConstant constant)
        {
            return constant switch
            {
                BuiltInConstant.Auto => Auto,
                _ => ThrowHelper.UnexpectedValue<NsTextDimension>(nameof(constant))
            };
        }
    }
}
