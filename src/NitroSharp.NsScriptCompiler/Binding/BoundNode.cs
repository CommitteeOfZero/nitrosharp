using System.Collections.Immutable;
using NitroSharp.NsScriptNew.Symbols;

namespace NitroSharp.NsScriptNew.Binding
{
    public abstract class BoundNode
    {
        public abstract BoundNodeKind Kind { get; }
    }

    public enum BoundNodeKind
    {
        Block,
        IfStatement,
        WhileStatement,
        FunctionCall,
        ExpressionStatement,
        ReturnStatement,
        SelectStatement,
        SelectSection,
        BreakStatement,

        Literal,
        Parameter,
        UnaryOperation,
        BinaryOperation,
        Assignment,
        DeltaExpression,

        CallStatement,
        CallExpression,
        Variable,
        BuiltInFunctionCall
    }

    public abstract class BoundExpression : BoundNode
    {
    }

    public abstract class BoundStatement : BoundNode
    {
    }

    public sealed class ExpressionStatement : BoundStatement
    {
        public ExpressionStatement(BoundExpression expression)
        {
            Expression = expression;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;

        public BoundExpression Expression { get; }
    }

    public sealed class BoundParameter : BoundExpression
    {
        public BoundParameter(ParameterSymbol symbol)
        {
            Symbol = symbol;
        }

        public override BoundNodeKind Kind => BoundNodeKind.Parameter;
        public ParameterSymbol Symbol { get; }
    }

    public sealed class BoundBlock : BoundStatement
    {
        public BoundBlock(ImmutableArray<BoundStatement> statements)
        {
            Statements = statements;
        }

        public ImmutableArray<BoundStatement> Statements { get; }
        public override BoundNodeKind Kind => BoundNodeKind.Block;
    }

    public sealed class IfStatement : BoundStatement
    {
        public IfStatement(
            BoundExpression condition,
            BoundStatement consequence,
            BoundStatement alternative)
        {
            Condition = condition;
            Consequence = consequence;
            Alternative = alternative;
        }

        public BoundExpression Condition { get; }
        public BoundStatement Consequence { get; }
        public BoundStatement Alternative { get; }

        public override BoundNodeKind Kind => BoundNodeKind.IfStatement;
    }

    public sealed class BreakStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.BreakStatement;
    }

    public sealed class WhileStatement : BoundStatement
    {
        public WhileStatement(BoundExpression condition, BoundStatement body)
        {
            Condition = condition;
            Body = body;
        }

        public BoundExpression Condition { get; }
        public BoundStatement Body { get; }

        public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;
    }

    public sealed class ReturnStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;
    }

    public sealed class SelectStatement : BoundStatement
    {
        public SelectStatement(BoundBlock body)
        {
            Body = body;
        }

        public BoundBlock Body { get; }
        public override BoundNodeKind Kind => BoundNodeKind.SelectStatement;
    }

    public sealed class SelectSection : BoundNode
    {
        public SelectSection(BoundBlock body)
        {
            Body = body;
        }

        public BoundBlock Body { get; }
        public override BoundNodeKind Kind => BoundNodeKind.SelectSection;
    }

    public sealed class Literal : BoundExpression
    {
        public Literal(ConstantValue value)
        {
            Value = value;
        }

        public ConstantValue Value { get; }
        public override BoundNodeKind Kind => BoundNodeKind.Literal;
    }

    public sealed class UnaryOperation : BoundExpression
    {
        public UnaryOperation(BoundExpression operand, UnaryOperatorKind operatorKind)
        {
            Operand = operand;
            Operator = operatorKind;
        }

        public BoundExpression Operand { get; }
        public UnaryOperatorKind Operator { get; }

        public override BoundNodeKind Kind => BoundNodeKind.UnaryOperation;
    }

    public sealed class BinaryOperation : BoundExpression
    {
        public BinaryOperation(BoundExpression left, BinaryOperatorKind operatorKind, BoundExpression right)
        {
            Left = left;
            OperatorKind = operatorKind;
            Right = right;
        }

        public override BoundNodeKind Kind => BoundNodeKind.BinaryOperation;

        public BoundExpression Left { get; }
        public BinaryOperatorKind OperatorKind { get; }
        public BoundExpression Right { get; }
    }

    public sealed class AssignmentOperation : BoundExpression
    {
        public AssignmentOperation(string variableName, BoundExpression value)
        {
            VariableName = variableName;
            Value = value;
        }

        public override BoundNodeKind Kind => BoundNodeKind.Assignment;

        public string VariableName { get; }
        public BoundExpression Value { get; }
    }

    public sealed class DeltaExpression : BoundExpression
    {
        public DeltaExpression(BoundExpression expression)
        {
            Expression = expression;
        }

        public override BoundNodeKind Kind => BoundNodeKind.DeltaExpression;
        public BoundExpression Expression { get; }
    }

    public sealed class VariableExpression : BoundExpression
    {
        public VariableExpression(string variableName)
        {
            VariableName = variableName;
        }

        public override BoundNodeKind Kind => BoundNodeKind.Variable;

        public string VariableName { get; }
    }

    public sealed class FunctionCall : BoundExpression
    {
        public FunctionCall(MemberSymbol target, ImmutableArray<BoundExpression> arguments)
        {
            Target = target;
            Arguments = arguments;
        }

        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;

        public MemberSymbol Target { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
    }

    public sealed class BuiltInFunctionCall : BoundExpression
    {
        public BuiltInFunctionCall(BuiltInFunctionSymbol target, ImmutableArray<BoundExpression> arguments)
        {
            Target = target;
            Arguments = arguments;
        }

        public override BoundNodeKind Kind => BoundNodeKind.BuiltInFunctionCall;

        public BuiltInFunctionSymbol Target { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
    }
}
