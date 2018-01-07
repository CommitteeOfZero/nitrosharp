using NitroSharp.NsScript.Uitls;

namespace NitroSharp.NsScript.Syntax
{
    public static class OperatorInfo
    {
        public static bool IsPrefixUnary(UnaryOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case UnaryOperatorKind.Not:
                case UnaryOperatorKind.Minus:
                case UnaryOperatorKind.Plus:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsPostfixUnary(UnaryOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case UnaryOperatorKind.PostfixDecrement:
                case UnaryOperatorKind.PostfixIncrement:
                    return true;

                default:
                    return false;
            }
        }

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
                    throw ExceptionUtils.UnexpectedValue(nameof(operatorKind));
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

                default:
                    throw ExceptionUtils.UnexpectedValue(nameof(operatorKind));
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
                case UnaryOperatorKind.PostfixIncrement:
                    return "++";
                case UnaryOperatorKind.PostfixDecrement:
                    return "--";

                default:
                    throw ExceptionUtils.UnexpectedValue(nameof(operatorKind));
            }
        }
    }
}
