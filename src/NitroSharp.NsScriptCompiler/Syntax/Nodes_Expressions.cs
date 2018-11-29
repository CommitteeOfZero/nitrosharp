using System;
using System.Collections.Immutable;

namespace NitroSharp.NsScriptNew.Syntax
{
    public abstract class Expression : SyntaxNode
    {
    }

    public sealed class Literal : Expression
    {
        internal static readonly Literal Null = new Literal(ConstantValue.Null);
        internal static readonly Literal True = new Literal(ConstantValue.True);
        internal static readonly Literal False = new Literal(ConstantValue.False);

        internal Literal(ConstantValue value)
        {
            Value = value;
        }

        public ConstantValue Value { get; }
        
        public override SyntaxNodeKind Kind => SyntaxNodeKind.Literal;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitLiteral(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitLiteral(this);
        }
    }

    public sealed class Identifier : Expression
    {
        internal Identifier(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.Identifier;

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
        internal UnaryExpression(Expression operand, UnaryOperatorKind operatorKind)
        {
            Operand = operand;
            OperatorKind = operatorKind;
        }

        public Expression Operand { get; }
        public UnaryOperatorKind OperatorKind { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.UnaryExpression;

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
        internal BinaryExpression(Expression left, BinaryOperatorKind operatorKind, Expression right)
        {
            Left = left;
            OperatorKind = operatorKind;
            Right = right;
        }

        public Expression Left { get; }
        public BinaryOperatorKind OperatorKind { get; }
        public Expression Right { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.BinaryExpression;

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
        internal AssignmentExpression(Expression target, AssignmentOperatorKind operatorKind, Expression value)
        {
            Target = target;
            OperatorKind = operatorKind;
            Value = value;
        }

        public Expression Target { get; }
        public AssignmentOperatorKind OperatorKind { get; }
        public Expression Value { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.AssignmentExpression;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitAssignmentExpression(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitAssignmentExpression(this);
        }
    }

    public sealed class DeltaExpression : Expression
    {
        internal DeltaExpression(Expression expression)
        {
            Expression = expression;
        }

        public Expression Expression { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.DeltaExpression;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitDeltaExpression(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitDeltaExpression(this);
        }
    }

    public sealed class FunctionCall : Expression
    {
        internal FunctionCall(string targetName, ImmutableArray<Expression> arguments)
        {
            Target = targetName;
            Arguments = arguments;
        }

        public string Target { get; }
        public ImmutableArray<Expression> Arguments { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.FunctionCall;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitFunctionCall(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitFunctionCall(this);
        }
    }
}
