using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax;

public abstract class SubroutineDeclaration : SyntaxNode
{
    protected SubroutineDeclaration(
        Spanned<string> name, Block body,
        ImmutableArray<DialogueBlock> dialogueBlocks, TextSpan span)
        : base(span)
    {
        Name = name;
        Body = body;
        DialogueBlocks = dialogueBlocks;
    }

    public Spanned<string> Name { get; }
    public Block Body { get; }
    public ImmutableArray<DialogueBlock> DialogueBlocks { get; }

    protected override SyntaxNode? GetNodeSlot(int index)
    {
        return index switch
        {
            0 => Body,
            _ => index < DialogueBlocks.Length ? DialogueBlocks[index] : null,
        };
    }
}

public sealed class ChapterDeclaration : SubroutineDeclaration
{
    internal ChapterDeclaration(
        Spanned<string> name, Block body,
        ImmutableArray<DialogueBlock> dialogueBlocks, TextSpan span)
        : base(name, body, dialogueBlocks, span)
    {
    }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ChapterDeclaration;

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitChapter(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitChapter(this);
    }
}

public sealed class SceneDeclaration : SubroutineDeclaration
{
    internal SceneDeclaration(
        Spanned<string> name, Block body,
        ImmutableArray<DialogueBlock> dialogueBlocks, TextSpan span)
        : base(name, body, dialogueBlocks, span)
    {
    }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.SceneDeclaration;

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitScene(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitScene(this);
    }
}

public sealed class FunctionDeclaration : SubroutineDeclaration
{
    internal FunctionDeclaration(
        Spanned<string> name, ImmutableArray<Parameter> parameters,
        Block body, ImmutableArray<DialogueBlock> dialogueBlocks,
        TextSpan span) : base(name, body, dialogueBlocks, span)
    {
        Parameters = parameters;
    }

    public ImmutableArray<Parameter> Parameters { get; }
    public override SyntaxNodeKind Kind => SyntaxNodeKind.FunctionDeclaration;

    protected override SyntaxNode? GetNodeSlot(int index)
    {
        return index switch
        {
            0 => Body,
            _ when index <= Parameters.Length => Parameters[index - 1],
            _ => base.GetNodeSlot(index)
        };
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitFunction(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitFunction(this);
    }
}

public sealed class Parameter : SyntaxNode
{
    internal Parameter(string name, TextSpan span) : base(span)
    {
        Name = name;
    }

    public string Name { get; }
    public override SyntaxNodeKind Kind => SyntaxNodeKind.Parameter;

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitParameter(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitParameter(this);
    }
}
