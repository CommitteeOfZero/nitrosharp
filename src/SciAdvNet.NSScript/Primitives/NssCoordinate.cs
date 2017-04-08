namespace SciAdvNet.NSScript
{
    public struct NssCoordinate
    {
        public NssCoordinate(int value, NssPositionOrigin relativeTo = NssPositionOrigin.Zero)
        {
            Value = value;
            Origin = relativeTo;
        }

        public int Value { get; }
        public NssPositionOrigin Origin { get; }
    }
}
