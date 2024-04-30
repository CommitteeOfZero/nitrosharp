namespace NitroSharp.Graphics;

internal abstract class ConstraintBox : RenderItem
{
    protected ConstraintBox(EntityName name, Entity? parent, int priority)
        : base(name, parent, priority)
    {
    }

    public abstract bool IsContainer { get; }
}
