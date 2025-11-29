using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax;

public sealed class DialogueBlock : Statement
{
    internal DialogueBlock(
        string name,
        string associatedBox,
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

    protected override SyntaxNode? GetChild(int index)
    {
        return index < Parts.Length ? Parts[index] : null;
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitDialogueBlock(this);
    }
}

public abstract class DialogueBlockPart(TextSpan span) : SyntaxNode(span)
{
    public sealed class CodeBlock(ImmutableArray<Statement> statements, TextSpan span)
        : DialogueBlockPart(span)
    {
        public ImmutableArray<Statement> Statements { get; } = statements;
        public override SyntaxNodeKind Kind => SyntaxNodeKind.MarkupCodeBlock;

        protected override SyntaxNode? GetChild(int index)
        {
            return index < Statements.Length ? Statements[index] : null;
        }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitDialogueCodeBlock(this);
        }
    }

    public sealed class Markup(TextSpan span) : DialogueBlockPart(span)
    {
        public override SyntaxNodeKind Kind => SyntaxNodeKind.Markup;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitDialogueMarkup(this);
        }

        protected override SyntaxNode? GetChild(int index) => null;
    }

    public sealed class BlankLine(TextSpan span) : DialogueBlockPart(span)
    {
        public override SyntaxNodeKind Kind => SyntaxNodeKind.MarkupBlankLine;

        public override void Accept(SyntaxVisitor visitor)
        {
            throw new System.NotImplementedException();
        }

        protected override SyntaxNode? GetChild(int index) => null;
    }
}
