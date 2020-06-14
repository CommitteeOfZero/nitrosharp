namespace NitroSharp.Graphics
{
    internal abstract class ConstraintBox : RenderItem2D
    {
        protected ConstraintBox(
            in ResolvedEntityPath path,
            int priority,
            bool isContainer)
            : base(path, priority)
        {
            IsContainer = isContainer;
        }

        public bool IsContainer { get; }

        protected override AnimPropagationMode AnimPropagationMode
            => IsContainer ? AnimPropagationMode.All : AnimPropagationMode.None;
    }
}
