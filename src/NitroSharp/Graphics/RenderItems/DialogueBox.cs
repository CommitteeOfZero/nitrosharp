namespace NitroSharp.Graphics
{
    internal sealed class DialogueBox :  ConstraintBox
    {
        private readonly Size _size;

        public DialogueBox(
            in ResolvedEntityPath path,
            int priority,
            Size size,
            bool inheritTransform) : base(path, priority, inheritTransform)
        {
            _size = size;
        }

        public override Size GetUnconstrainedBounds(RenderContext ctx) => _size;
    }
}
