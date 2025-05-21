using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax;

public sealed class DialogueBlock : Statement
{
    internal DialogueBlock(
        string name, string associatedBox,
        ImmutableArray<DialogueBlockPart> parts,
        TextSpan span) : base(span)
    {
        Name = name;
        AssociatedBox = associatedBox;
        Parts = parts;
    }

    public string Name { get; }
    public string AssociatedBox { get; }
    public ImmutableArray<DialogueBlockPart> Parts { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.DialogueBlock;

    protected override SyntaxNode? GetNodeSlot(int index)
    {
        return index < Parts.Length ? Parts[index] : null;
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitDialogueBlock(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitDialogueBlock(this);
    }
}

public abstract class DialogueBlockPart(TextSpan span) : SyntaxNode(span)
{
    public sealed class Block(ImmutableArray<Statement> statements, TextSpan span) : DialogueBlockPart(span)
    {
        public ImmutableArray<Statement> Statements { get; } = statements;
        public override SyntaxNodeKind Kind => SyntaxNodeKind.MarkupCodeBlock;

        protected override SyntaxNode? GetNodeSlot(int index)
        {
            return index < Statements.Length ? Statements[index] : null;
        }

        public override void Accept(SyntaxVisitor visitor)
        {
            throw new System.NotImplementedException();
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            throw new System.NotImplementedException();
        }
    }

    public sealed class Markup(string text, TextSpan span) : DialogueBlockPart(span)
    {
        public string Text { get; } = text;
        public override SyntaxNodeKind Kind => SyntaxNodeKind.Markup;

        public override void Accept(SyntaxVisitor visitor)
        {
            throw new System.NotImplementedException();
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            throw new System.NotImplementedException();
        }
    }
}
