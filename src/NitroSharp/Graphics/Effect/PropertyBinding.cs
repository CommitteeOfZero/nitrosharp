namespace NitroSharp.Graphics
{
    internal sealed class PropertyBinding
    {
        public PropertyBinding(BoundResourceAttribute attribute, uint positionInResourceSet, uint bufferSize)
        {
            Attribute = attribute;
            PositionInResourceSet = positionInResourceSet;
            BufferSize = bufferSize;
        }

        public BoundResourceAttribute Attribute { get; }
        public uint PositionInResourceSet { get; }
        public uint BufferSize { get; }
    }
}
