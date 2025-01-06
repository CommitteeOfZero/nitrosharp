using System;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax;

public abstract class Statement(TextSpan span) : SyntaxNode(span);

public sealed class Block : Statement
{
    internal Block(ImmutableArray<Statement> statements, TextSpan span) : base(span)
    {
        Statements = statements;
    }

    public ImmutableArray<Statement> Statements { get; }
    public override SyntaxNodeKind Kind => SyntaxNodeKind.Block;

    protected override SyntaxNode? GetNodeSlot(int index)
    {
        return index < Statements.Length ? Statements[index] : null;
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitBlock(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitBlock(this);
    }
}

public class ExpressionStatement : Statement
{
    internal ExpressionStatement(Expression expression, TextSpan span) : base(span)
    {
        Expression = expression;
    }

    public Expression Expression { get; }
    public override SyntaxNodeKind Kind => SyntaxNodeKind.ExpressionStatement;

    protected override SyntaxNode? GetNodeSlot(int index)
    {
        return index switch
        {
            0 => Expression,
            _ => null
        };
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitExpressionStatement(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitExpressionStatement(this);
    }
}

public sealed class IfStatement : Statement
{
    internal IfStatement(
        Expression condition,
        Statement ifTrueStatement,
        Statement? ifFalseStatement,
        TextSpan span) : base(span)
    {
        Condition = condition;
        IfTrueStatement = ifTrueStatement;
        IfFalseStatement = ifFalseStatement;
    }

    public Expression Condition { get; }
    public Statement IfTrueStatement { get; }
    public Statement? IfFalseStatement { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.IfStatement;

    protected override SyntaxNode? GetNodeSlot(int index)
    {
        return index switch
        {
            0 => Condition,
            1 => IfTrueStatement,
            2 => IfFalseStatement,
            _ => null
        };
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitIfStatement(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitIfStatement(this);
    }
}

public sealed class BreakStatement : Statement
{
    internal BreakStatement(TextSpan span) : base(span)
    {
    }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.BreakStatement;

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitBreakStatement(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitBreakStatement(this);
    }
}

public sealed class WhileStatement : Statement
{
    internal WhileStatement(Expression condition, Statement body, TextSpan span)
        : base(span)
    {
        Condition = condition;
        Body = body;
    }

    public Expression Condition { get; }
    public Statement Body { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.WhileStatement;

    protected override SyntaxNode? GetNodeSlot(int index)
    {
        return index switch
        {
            0 => Condition,
            1 => Body,
            _ => null
        };
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitWhileStatement(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitWhileStatement(this);
    }
}

public sealed class ReturnStatement : Statement
{
    internal ReturnStatement(TextSpan span) : base(span)
    {
    }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.ReturnStatement;

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitReturnStatement(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitReturnStatement(this);
    }
}

public sealed class SelectStatement : Statement
{
    internal SelectStatement(Block body, TextSpan span)
        : base(span)
    {
        Body = body;
    }

    public Block Body { get; }
    public override SyntaxNodeKind Kind => SyntaxNodeKind.SelectStatement;

    protected override SyntaxNode? GetNodeSlot(int index)
    {
        return index switch
        {
            0 => Body,
            _ => null,
        };
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitSelectStatement(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitSelectStatement(this);
    }
}

public sealed class SelectSection : Statement
{
    internal SelectSection(Spanned<string> label, Block body, TextSpan span)
        : base(span)
    {
        Label = label;
        Body = body;
    }

    public Spanned<string> Label { get; }
    public Block Body { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.SelectSection;

    protected override SyntaxNode? GetNodeSlot(int index)
    {
        return index switch
        {
            0 => Body,
            _ => null
        };
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitSelectSection(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitSelectSection(this);
    }
}

public sealed class CallChapterStatement : Statement
{
    internal CallChapterStatement(Spanned<string> targetModule, TextSpan span)
        : base(span)
    {
        TargetModule = targetModule;
    }

    public Spanned<string> TargetModule { get; }
    public override SyntaxNodeKind Kind => SyntaxNodeKind.CallChapterStatement;

    public override void Accept(SyntaxVisitor visitor)
    {
        throw new NotImplementedException();
        //visitor.VisitCallChapterStatement(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }
}

public sealed class CallSceneStatement : Statement
{
    internal CallSceneStatement(
        Spanned<string>? targetFile,
        Spanned<string> targetScene,
        TextSpan span) : base(span)
    {
        TargetModule = targetFile;
        TargetScene = targetScene;
    }

    public Spanned<string>? TargetModule { get; }
    public Spanned<string> TargetScene { get; }
    public override SyntaxNodeKind Kind => SyntaxNodeKind.CallSceneStatement;

    public override void Accept(SyntaxVisitor visitor)
    {
        throw new NotImplementedException();
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }
}
