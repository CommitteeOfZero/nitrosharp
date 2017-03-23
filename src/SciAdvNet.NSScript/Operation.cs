using System;

namespace SciAdvNet.NSScript
{
    public struct Operation
    {
        private Operation(OperationCategory category, OperationKind kind)
        {
            Category = category;
            Kind = kind;
        }

        public OperationCategory Category { get; }
        public OperationKind Kind { get; }
        public OperationPrecedence Precedence => GetPrecedence();

        public static Operation PrefixUnary(SyntaxTokenKind operatorTokenKind)
        {
            if (!SyntaxFacts.TryGetPrefixUnaryOperationKind(operatorTokenKind, out var operationKind))
            {
                throw new ArgumentException(nameof(operatorTokenKind));
            }

            return new Operation(OperationCategory.PrefixUnary, operationKind);
        }

        public static Operation PostfixUnary(SyntaxTokenKind operatorTokenKind)
        {
            if (!SyntaxFacts.TryGetPostfixUnaryOperationKind(operatorTokenKind, out var operationKind))
            {
                throw new ArgumentException(nameof(operatorTokenKind));
            }

            return new Operation(OperationCategory.PostfixUnary, operationKind);
        }

        public static Operation Binary(SyntaxTokenKind operatorTokenKind)
        {
            if (!SyntaxFacts.TryGetBinaryOperationKind(operatorTokenKind, out var operationKind))
            {
                throw new ArgumentException(nameof(operatorTokenKind));
            }

            return new Operation(OperationCategory.Binary, operationKind);
        }

        public static Operation Assignment(SyntaxTokenKind operatorTokenKind)
        {
            if (!SyntaxFacts.TryGetAssignmentOperationKind(operatorTokenKind, out var operationKind))
            {
                throw new ArgumentException(nameof(operatorTokenKind));
            }

            return new Operation(OperationCategory.Assignment, operationKind);
        }

        private OperationPrecedence GetPrecedence()
        {
            switch (Kind)
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
                    return Category == OperationCategory.Assignment ? OperationPrecedence.Assignment : OperationPrecedence.Unary;
            }
        }

        public override string ToString()
        {
            switch (Kind)
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
        Invalid = 0,

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

    //public static class OperationStatic
    //{
    //    public static string GetText(UnaryOperationKind unaryOperationKind)
    //    {
    //        switch (unaryOperationKind)
    //        {
    //            case UnaryOperationKind.LogicalNegation:
    //                return "!";
    //            case UnaryOperationKind.UnaryPlus:
    //                return "+";
    //            case UnaryOperationKind.UnaryMinus:
    //                return "-";
    //            case UnaryOperationKind.PostfixIncrement:
    //                return "++";
    //            case UnaryOperationKind.PostfixDecrement:
    //                return "--";

    //            default:
    //                return string.Empty;
    //        }
    //    }



    //    public static bool IsPrefixOperation(UnaryOperationKind unaryOperationKind)
    //    {
    //        switch (unaryOperationKind)
    //        {
    //            case UnaryOperationKind.LogicalNegation:
    //            case UnaryOperationKind.UnaryPlus:
    //            case UnaryOperationKind.UnaryMinus:
    //                return true;

    //            default:
    //                return false;
    //        }
    //    }

    //    public static bool IsPostfixOperation(UnaryOperationKind unaryOperationKind) => !IsPrefixOperation(unaryOperationKind);
    //}
}
