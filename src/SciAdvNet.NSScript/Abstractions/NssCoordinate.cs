namespace SciAdvNet.NSScript
{
    public struct NssCoordinate
    {
        public NssCoordinate(int value, NssRelativePosition relativeTo = NssRelativePosition.Zero)
        {
            Value = value;
            RelativeTo = relativeTo;
        }

        public int Value { get; }
        public NssRelativePosition RelativeTo { get; }

    }
}
