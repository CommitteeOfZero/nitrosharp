namespace NitroSharp.NsScript.Primitives
{
    public enum NsCoordinateOrigin
    {
        Zero,
        CurrentValue,
        Left,
        Top,
        Right,
        Bottom,
        Center
    }

    public readonly struct NsCoordinate
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

        public static NsCoordinate FromEnumValue(BuiltInConstant constant)
        {
            switch (constant)
            {
                case BuiltInConstant.InLeft:
                    return new NsCoordinate(0, NsCoordinateOrigin.Left, 0.0f);
                case BuiltInConstant.OnLeft:
                    return new NsCoordinate(0, NsCoordinateOrigin.Left, 0.5f);
                case BuiltInConstant.OutLeft:
                case BuiltInConstant.Left:
                    return new NsCoordinate(0, NsCoordinateOrigin.Left, 1.0f);

                case BuiltInConstant.InTop:
                    return new NsCoordinate(0, NsCoordinateOrigin.Top, 0.0f);
                case BuiltInConstant.OnTop:
                    return new NsCoordinate(0, NsCoordinateOrigin.Top, 0.5f);
                case BuiltInConstant.OutTop:
                case BuiltInConstant.Top:
                    return new NsCoordinate(0, NsCoordinateOrigin.Top, 1.0f);

                case BuiltInConstant.InRight:
                    return new NsCoordinate(0, NsCoordinateOrigin.Right, 1.0f);
                case BuiltInConstant.OnRight:
                    return new NsCoordinate(0, NsCoordinateOrigin.Right, 0.5f);
                case BuiltInConstant.OutRight:
                case BuiltInConstant.Right:
                    return new NsCoordinate(0, NsCoordinateOrigin.Right, 0.0f);

                case BuiltInConstant.InBottom:
                    return new NsCoordinate(0, NsCoordinateOrigin.Bottom, 1.0f);
                case BuiltInConstant.OnBottom:
                    return new NsCoordinate(0, NsCoordinateOrigin.Bottom, 0.5f);
                case BuiltInConstant.OutBottom:
                case BuiltInConstant.Bottom:
                    return new NsCoordinate(0, NsCoordinateOrigin.Bottom, 0.0f);

                case BuiltInConstant.Center:
                case BuiltInConstant.Middle:
                    return new NsCoordinate(0, NsCoordinateOrigin.Center, 0.5f);

                default:
                    throw ThrowHelper.UnexpectedValue(nameof(constant));
            }
        }
    }
}
