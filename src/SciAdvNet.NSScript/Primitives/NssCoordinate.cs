namespace SciAdvNet.NSScript
{
    public struct Coordinate
    {
        public Coordinate(float value, CoordinateOrigin origin, Rational anchorPoint)
        {
            Value = value;
            Origin = origin;
            AnchorPoint = anchorPoint;
        }

        public float Value { get; }
        public CoordinateOrigin Origin { get; }
        public Rational AnchorPoint { get; }
    }

    public enum CoordinateOrigin
    {
        Zero,
        CurrentValue,
        Left,
        Top,
        Right,
        Bottom,
        Center
    }
}
