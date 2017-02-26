using System;

namespace SciAdvNet.NSScript.Execution
{
    public sealed class ExpressionEvaluator
    {
        private readonly EvaluatingVisitor _evalVisitor;

        public ExpressionEvaluator()
        {
            _evalVisitor = new EvaluatingVisitor();
        }

        public ConstantValue EvaluateExpression(Expression expression, IFrame frame)
        {
            _evalVisitor.Frame = frame;
            return _evalVisitor.Visit(expression);
        }
    }

    internal sealed class EvaluatingVisitor : SyntaxVisitor<ConstantValue>
    {
        public IFrame Frame { get; set; }

        public override ConstantValue VisitLiteral(Literal literal)
        {
            return literal.Value;
        }

        public override ConstantValue VisitVariable(Variable variable)
        {
            return Frame.Globals[variable.Name.SimplifiedName];
        }

        public override ConstantValue VisitNamedConstant(NamedConstant namedConstant)
        {
            return new ConstantValue(namedConstant.Name.FullName);
        }

        public override ConstantValue VisitUnaryExpression(UnaryExpression unaryExpression)
        {
            return ApplyUnaryOperation(unaryExpression.Operand, unaryExpression.OperationKind);
        }

        public override ConstantValue VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            string targetName = assignmentExpression.Target.Name.SimplifiedName;
            var value = Visit(assignmentExpression.Value);
            Frame.Globals[targetName] = value;
            return value;
        }

        public override ConstantValue VisitBinaryExpression(BinaryExpression binaryExpression)
        {
            var leftValue = Visit(binaryExpression.Left);
            var rightValue = Visit(binaryExpression.Right);

            return ApplyBinaryOperation(leftValue, binaryExpression.OperationKind, rightValue);
        }

        public override ConstantValue VisitConstantValue(ConstantValue constantValue)
        {
            return base.VisitConstantValue(constantValue);
        }

        public override ConstantValue VisitParameterReference(ParameterReference parameterReference)
        {
            return Frame.Arguments[parameterReference.ParameterName.SimplifiedName];
        }

        private static ConstantValue ApplyBinaryOperation(ConstantValue leftOperand, BinaryOperationKind operationKind, ConstantValue rightOperand)
        {
            switch (operationKind)
            {
                case BinaryOperationKind.Addition:
                    return leftOperand + rightOperand;
                case BinaryOperationKind.Subtraction:
                    return leftOperand - rightOperand;
                case BinaryOperationKind.Multiplication:
                    return leftOperand * rightOperand;
                case BinaryOperationKind.Division:
                    return leftOperand / rightOperand;
                case BinaryOperationKind.Equal:
                    return leftOperand == rightOperand;
                case BinaryOperationKind.NotEqual:
                    return leftOperand != rightOperand;
                case BinaryOperationKind.LessThan:
                    return leftOperand < rightOperand;
                case BinaryOperationKind.LessThanOrEqual:
                    return leftOperand <= rightOperand;
                case BinaryOperationKind.GreaterThan:
                    return leftOperand > rightOperand;
                case BinaryOperationKind.GreaterThanOrEqual:
                    return leftOperand >= rightOperand;
                case BinaryOperationKind.LogicalAnd:
                    return leftOperand && rightOperand;
                case BinaryOperationKind.LogicalOr:
                default:
                    return leftOperand || rightOperand;
            }
        }

        private ConstantValue ApplyUnaryOperation(Expression operand, UnaryOperationKind operationKind)
        {
            if (operationKind == UnaryOperationKind.LogicalNegation)
            {
                return !Visit(operand);
            }

            if (operand.Kind != SyntaxNodeKind.Variable &&
                (operationKind == UnaryOperationKind.PostfixIncrement || operationKind == UnaryOperationKind.PostfixIncrement))
            {
                string op = Operation.GetText(operationKind);
                throw new InvalidOperationException($"Unary operator '{op}' can only be applied to variables.");
            }

            ConstantValue oldValue;

            string variableName = string.Empty;
            if (operand.Kind == SyntaxNodeKind.Variable)
            {
                variableName = (operand as Variable).Name.FullName;
                oldValue = Frame.Globals[((operand as Variable).Name.FullName)];
            }
            else
            {
                oldValue = Visit(operand);
            }

            switch (operationKind)
            {
                case UnaryOperationKind.UnaryPlus:
                    return oldValue;

                case UnaryOperationKind.UnaryMinus:
                    return -oldValue;

                case UnaryOperationKind.PostfixIncrement:
                    Frame.Globals[variableName] = oldValue++;
                    return oldValue;

                case UnaryOperationKind.PostfixDecrement:
                default:
                    Frame.Globals[variableName] = oldValue--;
                    return oldValue;
            }
        }
    }
}
