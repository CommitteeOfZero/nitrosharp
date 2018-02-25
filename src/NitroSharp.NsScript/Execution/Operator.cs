namespace NitroSharp.NsScript.Execution
{
    internal static class Operator
    {
        public static ConstantValue ApplyBinary(ConstantValue leftOperand, BinaryOperatorKind op, ConstantValue rightOperand)
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

        public static ConstantValue ApplyUnary(ConstantValue operand, UnaryOperatorKind op)
        {
            switch (op)
            {
                case UnaryOperatorKind.Not:
                    return !operand;

                case UnaryOperatorKind.Plus:
                    return operand;

                case UnaryOperatorKind.Minus:
                    return -operand;

                default:
                    throw ExceptionUtils.UnexpectedValue(nameof(op));
            }
        }

        public static ConstantValue Assign(Environment env, string variableName, ConstantValue value, AssignmentOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case AssignmentOperatorKind.Assign:
                default:
                    return value;

                case AssignmentOperatorKind.AddAssign:
                    return env.Get(variableName) + value;

                case AssignmentOperatorKind.SubtractAssign:
                    return env.Get(variableName) - value;

                case AssignmentOperatorKind.MultiplyAssign:
                    return env.Get(variableName) * value;

                case AssignmentOperatorKind.DivideAssign:
                    return env.Get(variableName) / value;

                case AssignmentOperatorKind.Increment:
                    return env.Get(variableName) + ConstantValue.One;

                case AssignmentOperatorKind.Decrement:
                    return env.Get(variableName) - ConstantValue.One;

            }
        }
    }
}
