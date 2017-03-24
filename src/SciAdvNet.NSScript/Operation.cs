using System;

namespace SciAdvNet.NSScript
{
    public static class Operation
    {
        //public static Operation PrefixUnary(SyntaxTokenKind operatorTokenKind)
        //{
        //    if (!SyntaxFacts.TryGetPrefixUnaryOperationKind(operatorTokenKind, out var operationKind))
        //    {
        //        throw new ArgumentException(nameof(operatorTokenKind));
        //    }

        //    return new Operation(operationKind);
        //}

        //public static Operation PostfixUnary(SyntaxTokenKind operatorTokenKind)
        //{
        //    if (!SyntaxFacts.TryGetPostfixUnaryOperationKind(operatorTokenKind, out var operationKind))
        //    {
        //        throw new ArgumentException(nameof(operatorTokenKind));
        //    }

        //    return new Operation(operationKind);
        //}

        //public static Operation Binary(SyntaxTokenKind operatorTokenKind)
        //{
        //    if (!SyntaxFacts.TryGetBinaryOperationKind(operatorTokenKind, out var operationKind))
        //    {
        //        throw new ArgumentException(nameof(operatorTokenKind));
        //    }

        //    return new Operation(operationKind);
        //}

        //public static Operation Assignment(SyntaxTokenKind operatorTokenKind)
        //{
        //    if (!SyntaxFacts.TryGetAssignmentOperationKind(operatorTokenKind, out var operationKind))
        //    {
        //        throw new ArgumentException(nameof(operatorTokenKind));
        //    }

        //    return new Operation(operationKind);
        //}

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

        public static bool IsAssignment(OperationKind operationKind)
        {
            switch (operationKind)
            {
                case OperationKind.SimpleAssignment:
                case OperationKind.AddAssignment:
                case OperationKind.SubtractAssignment:
                case OperationKind.MultiplyAssignment:
                case OperationKind.DivideAssignment:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsPrefixOperation(OperationKind operationKind)
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

        public static bool IsPostfixOperation(OperationKind operationKind) => !IsPrefixOperation(operationKind);
    }

    public enum OperationPrecedence : uint
    {
        Expression = 0,
        Assignment,
        Logical,
        Equality,
        Relational,
        Additive,
        Multiplicative,
        Unary
    }

    public enum OperationCategory
    {
        PrefixUnary,
        PostfixUnary,
        Binary,
        Assignment
    }

    public enum OperationKind
    {
        NoOp = 0,

        // Unary
        LogicalNegation,
        UnaryPlus,
        UnaryMinus,
        PostfixIncrement,
        PostfixDecrement,

        // Binary
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
        LogicalOr,

        // Assignment
        SimpleAssignment,
        AddAssignment,
        SubtractAssignment,
        MultiplyAssignment,
        DivideAssignment
    }
}
