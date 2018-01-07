using NitroSharp.NsScript.Symbols;
using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Uitls;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NitroSharp.NsScript.Execution
{
    public sealed class ExpressionEvaluator
    {
        private readonly EvaluatingVisitor _visitor;

        public ExpressionEvaluator(MemorySpace globals, IEngineImplementation builtInFunctionsImpl)
        {
            _visitor = new EvaluatingVisitor(globals, builtInFunctionsImpl);
        }

        public ConstantValue Evaluate(Expression expression, MemorySpace locals)
        {
            return _visitor.Evaluate(expression, locals);
        }

        private sealed class EvaluatingVisitor : SyntaxVisitor<ConstantValue>
        {
            private MemorySpace _globals, _locals;
            private IEngineImplementation _builtIns;

            public EvaluatingVisitor(MemorySpace globals, IEngineImplementation builtIns)
            {
                _globals = globals;
                _builtIns = builtIns;
            }

            public ConstantValue Evaluate(Expression expression, MemorySpace locals)
            {
                _locals = locals;
                return Visit(expression);
            }

            public override ConstantValue Visit(SyntaxNode node)
            {
                var result = base.Visit(node);
                Debug.Assert(!(result is null));
                return result;
            }

            public override ConstantValue VisitIdentifier(Identifier identifier)
            {
                switch (identifier.Symbol)
                {
                    case GlobalVariableSymbol _:
                        return _globals.Get(identifier.Value);

                    case ParameterSymbol parameter:
                        if (!_locals.TryGetValue(parameter.Name, out var value))
                        {
                            throw new System.Exception("Something's wrong.");
                        }
                        return value;

                    case BuiltInEnumValueSymbol enumValueSymbol:
                        return enumValueSymbol.Value;

                    default:
                        return ConstantValue.Create(identifier.Value);
                }
            }

            public override ConstantValue VisitLiteral(Literal literal)
            {
                return literal.Value;
            }

            public override ConstantValue VisitBinaryExpression(BinaryExpression binaryExpression)
            {
                var left = Visit(binaryExpression.Left);
                var right = Visit(binaryExpression.Right);

                return ApplyBinaryOperator(left, binaryExpression.OperatorKind, right);
            }

            public override ConstantValue VisitUnaryExpression(UnaryExpression unaryExpression)
            {
                var operand = Visit(unaryExpression.Operand);
                var result = ApplyUnaryOperator(operand, unaryExpression.OperatorKind);

                switch (unaryExpression.OperatorKind)
                {
                    case UnaryOperatorKind.PostfixIncrement:
                        string name = ((Identifier)unaryExpression.Operand).Value;
                        _globals.Set(name, _globals.Get(name) + ConstantValue.One);
                        break;

                    case UnaryOperatorKind.PostfixDecrement:
                        name = ((Identifier)unaryExpression.Operand).Value;
                        _globals.Set(name, _globals.Get(name) - ConstantValue.One);
                        break;
                }

                return result;
            }

            public override ConstantValue VisitAssignmentExpression(AssignmentExpression assignmentExpression)
            {
                var targetName = (Identifier)assignmentExpression.Target;
                var value = Visit(assignmentExpression.Value);

                MemorySpace memorySpace = targetName.Symbol.Kind == SymbolKind.GlobalVariable ? _globals : _locals;

                switch (assignmentExpression.OperatorKind)
                {
                    case AssignmentOperatorKind.Assign:
                        memorySpace.Set(targetName.Value, value);
                        break;

                    case AssignmentOperatorKind.AddAssign:
                        memorySpace.Set(targetName.Value, _globals.Get(targetName.Value) + value);
                        break;

                    case AssignmentOperatorKind.SubtractAssign:
                        memorySpace.Set(targetName.Value, _globals.Get(targetName.Value) - value);
                        break;

                    case AssignmentOperatorKind.MultiplyAssign:
                        memorySpace.Set(targetName.Value, _globals.Get(targetName.Value) * value);
                        break;

                    case AssignmentOperatorKind.DivideAssign:
                        memorySpace.Set(targetName.Value, _globals.Get(targetName.Value) / value);
                        break;

                }

                return value;
            }

            public override ConstantValue VisitDeltaExpression(DeltaExpression deltaExpression)
            {
                return ConstantValue.Create(Visit(deltaExpression.Expression).DoubleValue, isDeltaValue: true);
            }

            public override ConstantValue VisitFunctionCall(FunctionCall functionCall)
            {
                if (functionCall.Target.Symbol is BuiltInFunctionSymbol builtInFunction)
                {
                    var args = new Stack<ConstantValue>(functionCall.Arguments.Select(Visit).Reverse());
                    return builtInFunction.Implementation.Invoke(_builtIns, args);
                }

                return ConstantValue.Null;
            }

            private static ConstantValue ApplyBinaryOperator(ConstantValue leftOperand, BinaryOperatorKind op, ConstantValue rightOperand)
            {
                switch (op)
                {
                    case BinaryOperatorKind.Add:
                        return leftOperand + rightOperand;
                    case BinaryOperatorKind.Subtract:
                        return leftOperand - rightOperand;
                    case BinaryOperatorKind.Multiply:
                        return leftOperand * rightOperand;
                    case BinaryOperatorKind.Divide:
                        return leftOperand / rightOperand;
                    case BinaryOperatorKind.Equals:
                        return leftOperand == rightOperand;
                    case BinaryOperatorKind.NotEquals:
                        return leftOperand != rightOperand;
                    case BinaryOperatorKind.LessThan:
                        return leftOperand < rightOperand;
                    case BinaryOperatorKind.LessThanOrEqual:
                        return leftOperand <= rightOperand;
                    case BinaryOperatorKind.GreaterThan:
                        return leftOperand > rightOperand;
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return leftOperand >= rightOperand;
                    case BinaryOperatorKind.And:
                        return leftOperand && rightOperand;
                    case BinaryOperatorKind.Or:
                        return leftOperand || rightOperand;

                    case BinaryOperatorKind.Remainder:
                        return leftOperand % rightOperand;

                    default:
                        throw ExceptionUtils.UnexpectedValue(nameof(op));
                }
            }

            private static ConstantValue ApplyUnaryOperator(ConstantValue operand, UnaryOperatorKind op)
            {
                switch (op)
                {
                    case UnaryOperatorKind.Not:
                        return !operand;

                    case UnaryOperatorKind.Plus:
                        return operand;

                    case UnaryOperatorKind.Minus:
                        return -operand;

                    case UnaryOperatorKind.PostfixIncrement:
                    case UnaryOperatorKind.PostfixDecrement:
                        return operand;

                    default:
                        throw ExceptionUtils.UnexpectedValue(nameof(op));
                }
            }
        }
    }
}
