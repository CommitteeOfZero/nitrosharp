namespace SciAdvNet.NSScript
{
    public static class Operation
    {
        public static string GetText(UnaryOperationKind unaryOperationKind)
        {
            switch (unaryOperationKind)
            {
                case UnaryOperationKind.LogicalNegation:
                    return "!";
                case UnaryOperationKind.UnaryPlus:
                    return "+";
                case UnaryOperationKind.UnaryMinus:
                    return "-";
                case UnaryOperationKind.PostfixIncrement:
                    return "++";
                case UnaryOperationKind.PostfixDecrement:
                    return "--";

                default:
                    return string.Empty;
            }
        }

        public static string GetText(BinaryOperationKind binaryOperationKind)
        {
            switch (binaryOperationKind)
            {
                case BinaryOperationKind.Addition:
                    return "+";
                case BinaryOperationKind.Subtraction:
                    return "-";
                case BinaryOperationKind.Multiplication:
                    return "*";
                case BinaryOperationKind.Division:
                    return "/";
                case BinaryOperationKind.Equal:
                    return "==";
                case BinaryOperationKind.NotEqual:
                    return "!=";
                case BinaryOperationKind.LessThan:
                    return "<";
                case BinaryOperationKind.LessThanOrEqual:
                    return "<=";
                case BinaryOperationKind.GreaterThan:
                    return ">";
                case BinaryOperationKind.GreaterThanOrEqual:
                    return ">=";
                case BinaryOperationKind.LogicalAnd:
                    return "&&";
                case BinaryOperationKind.LogicalOr:
                    return "||";

                default:
                    return string.Empty;
            }
        }

        public static string GetText(AssignmentOperationKind assignmentOperationKind)
        {
            switch (assignmentOperationKind)
            {
                case AssignmentOperationKind.SimpleAssignment:
                    return "=";
                case AssignmentOperationKind.AddAssignment:
                    return "+=";
                case AssignmentOperationKind.SubtractAssignment:
                    return "-=";
                case AssignmentOperationKind.MultiplyAssignment:
                    return "*=";
                case AssignmentOperationKind.DivideAssignment:
                    return "/=";

                default:
                    return string.Empty;
            }
        }

        public static bool IsPrefixOperation(UnaryOperationKind unaryOperationKind)
        {
            switch (unaryOperationKind)
            {
                case UnaryOperationKind.LogicalNegation:
                case UnaryOperationKind.UnaryPlus:
                case UnaryOperationKind.UnaryMinus:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsPostfixOperation(UnaryOperationKind unaryOperationKind) => !IsPrefixOperation(unaryOperationKind);
    }

    public enum UnaryOperationKind
    {
        LogicalNegation,
        UnaryPlus,
        UnaryMinus,
        PostfixIncrement,
        PostfixDecrement,
    }

    public enum BinaryOperationKind
    {
        Multiplication,
        Division,
        Addition,
        Subtraction,
        Equal,
        NotEqual,
        LessThanOrEqual,
        GreaterThanOrEqual,
        LessThan,
        GreaterThan,
        LogicalAnd,
        LogicalOr
    }

    public enum AssignmentOperationKind
    {
        SimpleAssignment,
        AddAssignment,
        SubtractAssignment,
        MultiplyAssignment,
        DivideAssignment
    }
}
