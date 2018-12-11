using System;
using System.Collections.Immutable;
using NitroSharp.NsScriptNew.Symbols;
using NitroSharp.NsScriptNew.Syntax;

namespace NitroSharp.NsScriptNew.Binding
{
    public class Binder
    {
        private readonly Binder _parent;

        protected Binder(Binder parent)
        {
            _parent = parent;
        }

        public BoundBlock BindBlock(BlockSyntax blockSyntax)
        {
            ImmutableArray<StatementSyntax> stmtSyntaxNodes = blockSyntax.Statements;
            var statements = ImmutableArray.CreateBuilder<BoundStatement>(stmtSyntaxNodes.Length);
            foreach (StatementSyntax statementSyntax in stmtSyntaxNodes)
            {
                statements.Add(BindStatement(statementSyntax));
            }

            return new BoundBlock(statements.ToImmutable());
        }

        private BoundStatement BindStatement(StatementSyntax statement)
        {
            switch (statement.Kind)
            {
                case SyntaxNodeKind.Block:
                    return BindBlock((BlockSyntax)statement);
                case SyntaxNodeKind.ExpressionStatement:
                    return BindExpressionStatement((ExpressionStatementSyntax)statement);
                case SyntaxNodeKind.IfStatement:
                    return BindIfStatement((IfStatementSyntax)statement);
                case SyntaxNodeKind.BreakStatement:
                    return BindBreakStatement((BreakStatementSyntax)statement);
                case SyntaxNodeKind.WhileStatement:
                    return BindWhileStatement((WhileStatementSyntax)statement);
                case SyntaxNodeKind.ReturnStatement:
                    return BindReturnStatement((ReturnStatementSyntax)statement);
            }

            return null;
        }

        private ExpressionStatement BindExpressionStatement(ExpressionStatementSyntax statement)
        {
            return new ExpressionStatement(BindExpression(statement.Expression));
        }

        private WhileStatement BindWhileStatement(WhileStatementSyntax syntax)
        {
            BoundExpression condition = BindExpression(syntax.Condition);
            BoundStatement body = BindStatement(syntax.Body);
            return new WhileStatement(condition, body);
        }

        private ReturnStatement BindReturnStatement(ReturnStatementSyntax statement)
        {
            return new ReturnStatement();
        }

        private BreakStatement BindBreakStatement(BreakStatementSyntax statement)
        {
            return new BreakStatement();
        }

        private IfStatement BindIfStatement(IfStatementSyntax statement)
        {
            BoundExpression condition = BindExpression(statement.Condition);
            BoundStatement consequence = BindStatement(statement.IfTrueStatement);
            BoundStatement alternative = null;
            if (statement.IfFalseStatement != null)
            {
                alternative = BindStatement(statement.IfFalseStatement);
            }

            return new IfStatement(condition, consequence, alternative);
        }

        public BoundExpression BindExpression(ExpressionSyntax expression)
        {
            switch (expression.Kind)
            {
                case SyntaxNodeKind.LiteralExpression:
                    return BindLiteral((LiteralExpressionSyntax)expression);
                case SyntaxNodeKind.NameExpression:
                    return BindNameExpression((NameExpressionSyntax)expression);
                case SyntaxNodeKind.UnaryExpression:
                    return BindUnaryExpression((UnaryExpressionSyntax)expression);
                case SyntaxNodeKind.BinaryExpression:
                    return BindBinaryExpression((BinaryExpressionSyntax)expression);
                case SyntaxNodeKind.AssignmentExpression:
                    return BindAssignmentExpression((AssignmentExpressionSyntax)expression);
                case SyntaxNodeKind.DeltaExpression:
                    return BindDeltaExpression((DeltaExpressionSyntax)expression);
                case SyntaxNodeKind.FunctionCallExpression:
                    return BindFunctionCall((FunctionCallExpressionSyntax)expression);
            }

            return null;
        }

        private BoundExpression BindFunctionCall(FunctionCallExpressionSyntax expression)
        {
            Symbol symbol = LookupFunction(expression.TargetName.Value);
            if (symbol == null) { return null; }

            var arguments = ImmutableArray<BoundExpression>.Empty;
            ImmutableArray<ExpressionSyntax> argsSyntax = expression.Arguments;
            if (argsSyntax.Length > 0)
            {
                var builder = ImmutableArray.CreateBuilder<BoundExpression>(argsSyntax.Length);
                foreach (ExpressionSyntax argSyntax in argsSyntax)
                {
                    builder.Add(BindExpression(argSyntax));
                }

                arguments = builder.ToImmutable();
            }

            return symbol.Kind == SymbolKind.BuiltInFunction
                ? (BoundExpression)new BuiltInFunctionCall((BuiltInFunctionSymbol)symbol, arguments)
                : new FunctionCall((FunctionSymbol)symbol, arguments);
        }

        internal virtual Symbol LookupFunction(string name)
        {
            return GlobalBinder.LookupBuiltInFunction(name);
        }

        private DeltaExpression BindDeltaExpression(DeltaExpressionSyntax expression)
        {
            return new DeltaExpression(BindExpression(expression.Expression));
        }

        private AssignmentOperation BindAssignmentExpression(AssignmentExpressionSyntax expression)
        {
            string targetName = (expression.Target as NameExpressionSyntax).Name.Value;
            return new AssignmentOperation(
                targetName,
                BindExpression(expression.Value));
        }

        private BinaryOperation BindBinaryExpression(BinaryExpressionSyntax expression)
        {
            return new BinaryOperation(
                BindExpression(expression.Left),
                expression.OperatorKind.Value,
                BindExpression(expression.Right));
        }

        private UnaryOperation BindUnaryExpression(UnaryExpressionSyntax expression)
        {
            return new UnaryOperation(
                BindExpression(expression.Operand),
                expression.OperatorKind.Value);
        }

        private BoundExpression BindNameExpression(NameExpressionSyntax nameExpression)
        {
            string name = nameExpression.Name.Value;
            ParameterSymbol parameter = LookupParameter(name);
            if (parameter != null)
            {
                return new BoundParameter(LookupParameter(name));
            }

            return new VariableExpression(name);
        }

        private Literal BindLiteral(LiteralExpressionSyntax expression)
        {
            ConstantValue value = expression.Value.Value;
            if (value.Type == BuiltInType.String)
            {
                string stringValue = value.StringValue;
                BuiltInEnumValue? constant = GlobalBinder.LookupBuiltInEnumValue(stringValue);
                if (constant.HasValue)
                {
                    return null;
                }
            }

            return new Literal(value);
        }

        protected virtual ParameterSymbol LookupParameter(string name) => null;
    }
}
