namespace NitroSharp.NsScript.Primitives
{
    public enum NsTextDimensionVariant
    {
        Auto,
        Inherit,
        Value
    }

    public readonly struct NsTextDimension
    {
        public readonly NsTextDimensionVariant Variant;
        public readonly int? Value;

        public NsTextDimension(NsTextDimensionVariant variant, int? value)
            => (Variant, Value) = (variant, value);

        public static NsTextDimension Auto
            => new NsTextDimension(NsTextDimensionVariant.Auto, null);

        public static NsTextDimension Inherit
            => new NsTextDimension(NsTextDimensionVariant.Inherit, null);

        public static NsTextDimension WithValue(int value)
            => new NsTextDimension(NsTextDimensionVariant.Value, value);

        public static NsTextDimension FromConstant(BuiltInConstant constant)
        {
            return constant switch
            {
                BuiltInConstant.Auto => Auto,
                BuiltInConstant.Inherit => Inherit,
                _ => ThrowHelper.UnexpectedValue<NsTextDimension>(nameof(constant))
            };
        }
    }
}
