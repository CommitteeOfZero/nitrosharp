namespace NitroSharp.Graphics;

internal sealed class DialogueBox : ConstraintBox
{
    public DialogueBox(EntityName name, Entity? parent, int priority) : base(name, parent, priority)
    {
    }

    public override DesignSize GetUnconstrainedBounds(RenderContext ctx)
    {
        throw new System.NotImplementedException();
    }

    public override bool IsContainer { get; }
}
