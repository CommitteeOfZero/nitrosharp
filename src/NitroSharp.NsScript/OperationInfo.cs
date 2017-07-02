using System;

namespace NitroSharp.NsScript
{
    public static class OperationInfo
    {
        public static OperationCategory GetCategory(OperationKind operationKind)
        {
            switch (operationKind)
            {
                case OperationKind.LogicalNegation:
                case OperationKind.UnaryPlus:
                case OperationKind.UnaryMinus:
                case OperationKind.PostfixDecrement:
                case OperationKind.PostfixIncrement:
                    return OperationCategory.Unary;

                case OperationKind.Multiplication:
                case OperationKind.Division:
                case OperationKind.Addition:
                case OperationKind.Subtraction:
                case OperationKind.Equal:
                case OperationKind.NotEqual:
                case OperationKind.LessThanOrEqual:
                case OperationKind.GreaterThanOrEqual:
                case OperationKind.LessThan:
                case OperationKind.GreaterThan:
                case OperationKind.LogicalAnd:
                case OperationKind.LogicalOr:
                    return OperationCategory.Binary;

                case OperationKind.SimpleAssignment:
                case OperationKind.AddAssignment:
                case OperationKind.SubtractAssignment:
                case OperationKind.MultiplyAssignment:
                case OperationKind.DivideAssignment:
                    return OperationCategory.Assignment;

                case OperationKind.NoOp:
                default:
                    return OperationCategory.None;
            }
        }

        public static bool IsUnary(OperationKind operationKind) => GetCategory(operationKind) == OperationCategory.Unary;
        public static bool IsBinary(OperationKind operationKind) => GetCategory(operationKind) == OperationCategory.Binary;
        public static bool IsAssignment(OperationKind operationKind) => GetCategory(operationKind) == OperationCategory.Assignment;

        public static bool IsPrefixUnary(OperationKind operationKind)
        {
            switch (operationKind)
            {
                case OperationKind.LogicalNegation:
                case OperationKind.UnaryPlus:
                case OperationKind.UnaryMinus:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsPostfixUnary(OperationKind operationKind)
        {
            switch (operationKind)
            {
                case OperationKind.PostfixDecrement:
                case OperationKind.PostfixIncrement:
                    return true;

                default:
                    return false;
            }
        }

        public static OperationPrecedence GetPrecedence(OperationKind operationKind)
        {
            switch (operationKind)
            {
                case OperationKind.Multiplication:
                case OperationKind.Division:
                    return OperationPrecedence.Multiplicative;

                case OperationKind.Addition:
                case OperationKind.Subtraction:
                    return OperationPrecedence.Additive;

                case OperationKind.GreaterThan:
                case OperationKind.GreaterThanOrEqual:
                case OperationKind.LessThan:
                case OperationKind.LessThanOrEqual:
                    return OperationPrecedence.Relational;

                case OperationKind.Equal:
                case OperationKind.NotEqual:
                    return OperationPrecedence.Equality;

                case OperationKind.LogicalAnd:
                case OperationKind.LogicalOr:
                    return OperationPrecedence.Logical;

                default:
                    return IsAssignment(operationKind) ? OperationPrecedence.Assignment : OperationPrecedence.Unary;
            }
        }

        public static string GetText(OperationKind operationKind)
        {
            switch (operationKind)
            {
                case OperationKind.LogicalNegation:
                    return "!";
                case OperationKind.UnaryPlus:
                    return "+";
                case OperationKind.UnaryMinus:
                    return "-";
                case OperationKind.PostfixIncrement:
                    return "++";
                case OperationKind.PostfixDecrement:
                    return "--";

                case OperationKind.Addition:
                    return "+";
                case OperationKind.Subtraction:
                    return "-";
                case OperationKind.Multiplication:
                    return "*";
                case OperationKind.Division:
                    return "/";
                case OperationKind.Equal:
                    return "==";
                case OperationKind.NotEqual:
                    return "!=";
                case OperationKind.LessThan:
                    return "<";
                case OperationKind.LessThanOrEqual:
                    return "<=";
                case OperationKind.GreaterThan:
                    return ">";
                case OperationKind.GreaterThanOrEqual:
                    return ">=";
                case OperationKind.LogicalAnd:
                    return "&&";
                case OperationKind.LogicalOr:
                    return "||";

                case OperationKind.SimpleAssignment:
                    return "=";
                case OperationKind.AddAssignment:
                    return "+=";
                case OperationKind.SubtractAssignment:
                    return "-=";
                case OperationKind.MultiplyAssignment:
                    return "*=";
                case OperationKind.DivideAssignment:
                    return "/=";

                default:
                    return string.Empty;
            }
        }
    }
}
