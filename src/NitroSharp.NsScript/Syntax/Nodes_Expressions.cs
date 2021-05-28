using System;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax
{
    public abstract class Expression : SyntaxNode
    {
        protected Expression(TextSpan span) : base(span)
        {
        }
    }

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

        public override SyntaxNode? GetNodeSlot(int index)
        {
            switch (index)
            {
                case 0: return Operand;
                default: return null;
            }
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

        public override SyntaxNode? GetNodeSlot(int index)
        {
            switch (index)
            {
                case 0: return Left;
                case 1: return Right;
                default: return null;
            }
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

        public override SyntaxNode? GetNodeSlot(int index)
        {
            switch (index)
            {
                case 0: return Target;
                case 1: return Value;
                default: return null;
            }
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
        public BezierExpression(
            ImmutableArray<BezierControlPoint> controlPoints,
            TextSpan span) : base(span)
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

    public readonly struct BezierControlPoint
    {
        public readonly Expression X;
        public readonly Expression Y;
        public readonly bool IsStartingPoint;

        public BezierControlPoint(Expression x, Expression y, bool starting)
            => (X, Y, IsStartingPoint) = (x, y, starting);

        public void Deconstruct(out Expression x, out Expression y)
        {
            x = X;
            y = Y;
        }
    }
}
