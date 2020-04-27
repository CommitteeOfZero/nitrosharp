#nullable enable

namespace NitroSharp
{
    internal sealed class Choice : Entity
    {
        public Choice(in ResolvedEntityPath path)
            : base(in path)
        {
        }

        public EntityId DefaultVisual { get; set; }
        public EntityId MouseOverVisual { get; set; }
        public EntityId MouseDownVisual { get; set; }
        public EntityId MouseEnterThread { get; set; }
        public EntityId MouseLeaveThread { get; set; }
    }
}
