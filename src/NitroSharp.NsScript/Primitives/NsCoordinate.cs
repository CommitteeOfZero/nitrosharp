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
    }
}
