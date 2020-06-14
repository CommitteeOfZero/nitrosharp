namespace NitroSharp
{
    internal sealed class SimpleEntity : Entity
    {
        public SimpleEntity(in ResolvedEntityPath path) : base(path)
        {
        }

        public override bool IsIdle => true;
    }
}
