using System.Collections.Immutable;

namespace SciAdvNet.NSScript
{
    public abstract class Expression : SyntaxNode
    {
    }

    public static class ExpressionFactory
    {
        private static readonly Literal s_null = Literal("null", ConstantValue.Null);
        private static readonly Literal s_true = Literal("true", ConstantValue.True);
        private static readonly Literal s_false = Literal("false", ConstantValue.False);

        public static Literal Literal(string text, ConstantValue value) => new Literal(text, value);
        public static Literal Null => s_null;
        public static Literal True => s_true;
        public static Literal False => s_false;

        public static Identifier Identifier(string fullName, string simplifiedName, SigilKind sigil) =>
            new Identifier(fullName, simplifiedName, sigil);

        public static NamedConstant NamedConstant(Identifier name) => new NamedConstant(name);
        public static Variable Variable(Identifier name) => new Variable(name);

        public static ParameterReference ParameterReference(Identifier parameterName) =>
            new ParameterReference(parameterName);

        public static UnaryExpression Unary(Expression operand, OperationKind operationKind) => new UnaryExpression(operand, operationKind);

        public static BinaryExpression Binary(Expression left, OperationKind operationKind, Expression right)
            => new BinaryExpression(left, operationKind, right);

        public static AssignmentExpression Assignment(Variable target, OperationKind operationKind, Expression value)
            => new AssignmentExpression(target, operationKind, value);

        public static FunctionCall FunctionCall(Identifier targetFunctionName, ImmutableArray<Expression> arguments) =>
            new FunctionCall(targetFunctionName, arguments);
    }

    public sealed class FunctionCall : Expression
    {
        internal FunctionCall(Identifier targetFunctionName, ImmutableArray<Expression> arguments)
        {
            TargetFunctionName = targetFunctionName;
            Arguments = arguments;
        }

        public Identifier TargetFunctionName { get; }
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

    public enum SigilKind
    {
        None,
        Dollar,
        Hash,
        At,
        Arrow
    }

    public sealed class Literal : Expression
    {
        internal Literal(string text, ConstantValue value)
        {
            Text = text;
            Value = value;
        }

        public ConstantValue Value { get; }
        public string Text { get; }
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

    public sealed class NamedConstant : Expression
    {
        internal NamedConstant(Identifier name)
        {
            Name = name;
        }

        public Identifier Name { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.NamedConstant;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitNamedConstant(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitNamedConstant(this);
        }
    }

    public sealed class Identifier : Expression
    {
        internal Identifier(string fullName, string simplifiedName, SigilKind sigil)
        {
            FullName = fullName;
            SimplifiedName = simplifiedName;
            Sigil = sigil;
        }

        public string FullName { get; }
        public string SimplifiedName { get; }
        public SigilKind Sigil { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.Identifier;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitIdentifier(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitIdentifier(this);
        }
    }

    public class Variable : Expression
    {
        internal Variable(Identifier name)
        {
            Name = name;
        }

        public Identifier Name { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.Variable;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitVariable(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitVariable(this);
        }
    }

    public class ParameterReference : Expression
    {
        internal ParameterReference(Identifier parameterName)
        {
            ParameterName = parameterName;
        }

        public Identifier ParameterName { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.Parameter;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitParameterReference(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitParameterReference(this);
        }
    }

    public sealed class UnaryExpression : Expression
    {
        internal UnaryExpression(Expression operand, OperationKind operationKind)
        {
            Operand = operand;
            OperationKind = operationKind;
        }

        public Expression Operand { get; }
        public OperationKind OperationKind { get; }

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
        internal BinaryExpression(Expression left, OperationKind operationKind, Expression right)
        {
            Left = left;
            OperationKind = operationKind;
            Right = right;
        }

        public Expression Left { get; }
        public OperationKind OperationKind { get; }
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
        internal AssignmentExpression(Variable target, OperationKind operationKind, Expression value)
        {
            Target = target;
            OperationKind = operationKind;
            Value = value;
        }

        public Variable Target { get; }
        public OperationKind OperationKind { get; }
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
}
