namespace NitroSharp.NsScript.Primitives
{
    public enum NsDimensionVariant
    {
        Auto,
        Inherit,
        Value
    }

    public readonly struct NsDimension
    {
        public readonly NsDimensionVariant Variant;
        public readonly int? Value;

        public NsDimension(NsDimensionVariant variant, int? value)
            => (Variant, Value) = (variant, value);

        public static NsDimension Auto
            => new NsDimension(NsDimensionVariant.Auto, null);

        public static NsDimension Inherit
            => new NsDimension(NsDimensionVariant.Inherit, null);

        public static NsDimension WithValue(int value)
            => new NsDimension(NsDimensionVariant.Value, value);

        public static NsDimension FromConstant(BuiltInConstant constant)
        {
            return constant switch
            {
                BuiltInConstant.Auto => Auto,
                BuiltInConstant.Inherit => Inherit,
                _ => ThrowHelper.UnexpectedValue<NsDimension>(nameof(constant))
            };
        }
    }
}
