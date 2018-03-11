namespace NitroSharp.Graphics
{
    internal sealed class PropertyBinding
    {
        public PropertyBinding(string resourceName, BoundResourceAttribute attribute, uint positionInResourceSet, uint bufferSize)
        {
            ResourceName = resourceName;
            Attribute = attribute;
            PositionInResourceSet = positionInResourceSet;
            BufferSize = bufferSize;
        }

        public string ResourceName { get; }
        public BoundResourceAttribute Attribute { get; }
        public uint PositionInResourceSet { get; }
        public uint BufferSize { get; }
    }
}
