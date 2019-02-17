namespace NitroSharp.NsScript
{
    public static class OperatorInfo
    {
        public static string GetText(BinaryOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case BinaryOperatorKind.Add:
                    return "+";
                case BinaryOperatorKind.Subtract:
                    return "-";
                case BinaryOperatorKind.Multiply:
                    return "*";
                case BinaryOperatorKind.Divide:
                    return "/";
                case BinaryOperatorKind.Remainder:
                    return "%";
                case BinaryOperatorKind.Equals:
                    return "==";
                case BinaryOperatorKind.NotEquals:
                    return "!=";
                case BinaryOperatorKind.LessThan:
                    return "<";
                case BinaryOperatorKind.LessThanOrEqual:
                    return "<=";
                case BinaryOperatorKind.GreaterThan:
                    return ">";
                case BinaryOperatorKind.GreaterThanOrEqual:
                    return ">=";
                case BinaryOperatorKind.And:
                    return "&&";
                case BinaryOperatorKind.Or:
                    return "||";

                default:
                    throw ThrowHelper.UnexpectedValue(nameof(operatorKind));
            }
        }

        public static string GetText(AssignmentOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case AssignmentOperatorKind.Assign:
                    return "=";
                case AssignmentOperatorKind.AddAssign:
                    return "+=";
                case AssignmentOperatorKind.SubtractAssign:
                    return "-=";
                case AssignmentOperatorKind.MultiplyAssign:
                    return "*=";
                case AssignmentOperatorKind.DivideAssign:
                    return "/=";
                case AssignmentOperatorKind.Increment:
                    return "++";
                case AssignmentOperatorKind.Decrement:
                    return "--";

                default:
                    throw ThrowHelper.UnexpectedValue(nameof(operatorKind));
            }
        }

        public static string GetText(UnaryOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case UnaryOperatorKind.Not:
                    return "!";
                case UnaryOperatorKind.Plus:
                    return "+";
                case UnaryOperatorKind.Minus:
                    return "-";

                default:
                    throw ThrowHelper.UnexpectedValue(nameof(operatorKind));
            }
        }
    }
}
