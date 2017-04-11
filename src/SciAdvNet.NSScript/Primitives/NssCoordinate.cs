namespace SciAdvNet.NSScript
{
    public struct Coordinate
    {
        public Coordinate(int value, CoordinateOrigin origin, float anchorPoint)
        {
            Value = value;
            Origin = origin;
            AnchorPoint = anchorPoint;
        }

        public int Value { get; }
        public CoordinateOrigin Origin { get; }
        public float AnchorPoint { get; }
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
