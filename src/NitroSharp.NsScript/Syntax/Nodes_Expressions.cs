using System;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax;

public abstract class Expression(TextSpan span) : SyntaxNode(span);

public sealed class LiteralExpression : Expression
{
    internal LiteralExpression(in ConstantValue value, TextSpan span) : base(span)
    {
        Value = value;
    }

    public ConstantValue Value { get; }
    public override SyntaxNodeKind Kind => SyntaxNodeKind.LiteralExpression;

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitLiteral(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitLiteral(this);
    }
}

public sealed class NameExpression : Expression
{
    internal NameExpression(string name, SigilKind sigil, TextSpan span) : base(span)
    {
        Name = name;
        Sigil = sigil;
    }

    public string Name { get; }
    public SigilKind Sigil { get; }
    public override SyntaxNodeKind Kind => SyntaxNodeKind.NameExpression;

    public override void Accept(SyntaxVisitor visitor)
    {
        //visitor.VisitIdentifier(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
        //return visitor.VisitIdentifier(this);
    }
}

public sealed class UnaryExpression : Expression
{
    internal UnaryExpression(
        Expression operand,
        Spanned<UnaryOperatorKind> operatorKind,
        TextSpan span) : base(span)
    {
        Operand = operand;
        OperatorKind = operatorKind;
    }

    public Expression Operand { get; }
    public Spanned<UnaryOperatorKind> OperatorKind { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.UnaryExpression;

    protected override SyntaxNode? GetNodeSlot(int index)
    {
        return index switch
        {
            0 => Operand,
            _ => null
        };
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitUnaryExpression(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitUnaryExpression(this);
    }
}

public sealed class BinaryExpression : Expression
{
    internal BinaryExpression(
        Expression left,
        Spanned<BinaryOperatorKind> operatorKind,
        Expression right,
        TextSpan span) : base(span)
    {
        Left = left;
        OperatorKind = operatorKind;
        Right = right;
    }

    public Expression Left { get; }
    public Spanned<BinaryOperatorKind> OperatorKind { get; }
    public Expression Right { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.BinaryExpression;

    protected override SyntaxNode? GetNodeSlot(int index)
    {
        return index switch
        {
            0 => Left,
            1 => Right,
            _ => null
        };
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitBinaryExpression(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitBinaryExpression(this);
    }
}

public sealed class AssignmentExpression : Expression
{
    internal AssignmentExpression(
        Expression target,
        Spanned<AssignmentOperatorKind> operatorKind,
        Expression value,
        TextSpan span) : base(span)
    {
        Target = target;
        OperatorKind = operatorKind;
        Value = value;
    }

    public Expression Target { get; }
    public Spanned<AssignmentOperatorKind> OperatorKind { get; }
    public Expression Value { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.AssignmentExpression;

    protected override SyntaxNode? GetNodeSlot(int index)
    {
        return index switch
        {
            0 => Target,
            1 => Value,
            _ => null
        };
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitAssignmentExpression(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitAssignmentExpression(this);
    }
}

public sealed class FunctionCallExpression : Expression
{
    internal FunctionCallExpression(
        Spanned<string> targetName,
        ImmutableArray<Expression> arguments,
        TextSpan span) : base(span)
    {
        TargetName = targetName;
        Arguments = arguments;
    }

    public Spanned<string> TargetName { get; }
    public ImmutableArray<Expression> Arguments { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.FunctionCallExpression;

    protected override SyntaxNode? GetNodeSlot(int index)
    {
        return index < Arguments.Length ? Arguments[index] : null;
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitFunctionCall(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitFunctionCall(this);
    }
}

public sealed class BezierExpression : Expression
{
    public BezierExpression(ImmutableArray<BezierControlPoint> controlPoints, TextSpan span)
        : base(span)
    {
        ControlPoints = controlPoints;
    }

    public ImmutableArray<BezierControlPoint> ControlPoints { get; }
    public override SyntaxNodeKind Kind => SyntaxNodeKind.BezierExpression;

    public override void Accept(SyntaxVisitor visitor)
    {
        throw new NotImplementedException();
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }
}

public readonly struct BezierControlPoint(Expression x, Expression y, bool starting)
{
    public readonly Expression X = x;
    public readonly Expression Y = y;
    public readonly bool IsStartingPoint = starting;

    public void Deconstruct(out Expression x, out Expression y)
    {
        x = X;
        y = Y;
    }
}

public sealed class ErrorExpression(TextSpan span) : Expression(span)
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.ErrorExpression;

    public override void Accept(SyntaxVisitor visitor)
    {
        throw new NotImplementedException();
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }
}
