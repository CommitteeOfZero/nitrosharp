namespace NitroSharp.Graphics
{
    internal abstract class ConstraintBox : RenderItem2D
    {
        protected ConstraintBox(
            in ResolvedEntityPath path,
            int priority,
            bool inheritTransform)
            : base(path, priority)
        {
            InheritTransform = inheritTransform;
        }

        public bool InheritTransform { get; }
    }
}
