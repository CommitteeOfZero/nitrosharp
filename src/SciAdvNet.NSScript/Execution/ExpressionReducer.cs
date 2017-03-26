using System;

namespace SciAdvNet.NSScript.Execution
{
    public class ExpressionReducer
    {
        public Frame CurrentFrame { get; set; }

        public ConstantValue ReduceExpression(Expression expression)
        {
            switch (expression)
            {
                case ConstantValue constant:
                    return constant;
                case Literal literal:
                    return literal.Value;
                case Variable variable:
                    string name = variable.Name.SimplifiedName;
                    return CurrentFrame.Globals[name];
                case ParameterReference parameterRef:
                    return CurrentFrame.Arguments[parameterRef.ParameterName.SimplifiedName];
                case NamedConstant namedConstant:
                    return new ConstantValue(namedConstant.Name.FullName);

                default:
                    throw new InvalidOperationException($"Expression '{expression.ToString()}' can't be reduced.");
            }
        }

        public ConstantValue ApplyBinaryOperation(ConstantValue leftOperand, OperationKind op, ConstantValue rightOperand)
        {
            switch (op)
            {
                case OperationKind.Addition:
                    return leftOperand + rightOperand;
                case OperationKind.Subtraction:
                    return leftOperand - rightOperand;
                case OperationKind.Multiplication:
                    return leftOperand * rightOperand;
                case OperationKind.Division:
                    return leftOperand / rightOperand;
                case OperationKind.Equal:
                    return leftOperand == rightOperand;
                case OperationKind.NotEqual:
                    return leftOperand != rightOperand;
                case OperationKind.LessThan:
                    return leftOperand < rightOperand;
                case OperationKind.LessThanOrEqual:
                    return leftOperand <= rightOperand;
                case OperationKind.GreaterThan:
                    return leftOperand > rightOperand;
                case OperationKind.GreaterThanOrEqual:
                    return leftOperand >= rightOperand;
                case OperationKind.LogicalAnd:
                    return leftOperand && rightOperand;
                case OperationKind.LogicalOr:
                default:
                    return leftOperand || rightOperand;
            }
        }

        public ConstantValue ApplyUnaryOperation(Expression operand, OperationKind operationKind)
        {
            if (operationKind == OperationKind.LogicalNegation)
            {
                return !ReduceExpression(operand);
            }

            if (operand.Kind != SyntaxNodeKind.Variable &&
                (operationKind == OperationKind.PostfixIncrement || operationKind == OperationKind.PostfixIncrement))
            {
                string op = OperationInfo.GetText(operationKind);
                throw new InvalidOperationException($"Unary operator '{op}' can only be applied to variables.");
            }

            ConstantValue oldValue;
            string variableName = string.Empty;
            if (operand.Kind == SyntaxNodeKind.Variable)
            {
                variableName = (operand as Variable).Name.FullName;
                oldValue = CurrentFrame.Globals[variableName];
            }
            else
            {
                oldValue = ReduceExpression(operand);
            }

            switch (operationKind)
            {
                case OperationKind.UnaryPlus:
                    return oldValue;

                case OperationKind.UnaryMinus:
                    return -oldValue;

                case OperationKind.PostfixIncrement:
                    CurrentFrame.Globals[variableName] = oldValue++;
                    return oldValue;

                case OperationKind.PostfixDecrement:
                default:
                    CurrentFrame.Globals[variableName] = oldValue--;
                    return oldValue;
            }
        }
    }
}
