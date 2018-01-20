namespace NitroSharp.NsScript
{
    public struct NsCoordinate
    {
        public NsCoordinate(int value, NsCoordinateOrigin origin, float anchorPoint)
        {
            Value = value;
            Origin = origin;
            AnchorPoint = anchorPoint;
        }

        public int Value { get; }
        public NsCoordinateOrigin Origin { get; }
        public float AnchorPoint { get; }

        public static NsCoordinate FromEnumValue(BuiltInEnumValue constant)
        {
            switch (constant)
            {
                case BuiltInEnumValue.InLeft:
                    return new NsCoordinate(0, NsCoordinateOrigin.Left, 0.0f);
                case BuiltInEnumValue.OnLeft:
                    return new NsCoordinate(0, NsCoordinateOrigin.Left, 0.5f);
                case BuiltInEnumValue.OutLeft:
                case BuiltInEnumValue.Left:
                    return new NsCoordinate(0, NsCoordinateOrigin.Left, 1.0f);

                case BuiltInEnumValue.InTop:
                    return new NsCoordinate(0, NsCoordinateOrigin.Top, 0.0f);
                case BuiltInEnumValue.OnTop:
                    return new NsCoordinate(0, NsCoordinateOrigin.Top, 0.5f);
                case BuiltInEnumValue.OutTop:
                case BuiltInEnumValue.Top:
                    return new NsCoordinate(0, NsCoordinateOrigin.Top, 1.0f);

                case BuiltInEnumValue.InRight:
                    return new NsCoordinate(0, NsCoordinateOrigin.Right, 1.0f);
                case BuiltInEnumValue.OnRight:
                    return new NsCoordinate(0, NsCoordinateOrigin.Right, 0.5f);
                case BuiltInEnumValue.OutRight:
                case BuiltInEnumValue.Right:
                    return new NsCoordinate(0, NsCoordinateOrigin.Right, 0.0f);

                case BuiltInEnumValue.InBottom:
                    return new NsCoordinate(0, NsCoordinateOrigin.Bottom, 1.0f);
                case BuiltInEnumValue.OnBottom:
                    return new NsCoordinate(0, NsCoordinateOrigin.Bottom, 0.5f);
                case BuiltInEnumValue.OutBottom:
                case BuiltInEnumValue.Bottom:
                    return new NsCoordinate(0, NsCoordinateOrigin.Bottom, 0.0f);

                case BuiltInEnumValue.Center:
                case BuiltInEnumValue.Middle:
                    return new NsCoordinate(0, NsCoordinateOrigin.Center, 0.5f);

                default:
                    throw ExceptionUtils.UnexpectedValue(nameof(constant));
            }
        }
    }
}
