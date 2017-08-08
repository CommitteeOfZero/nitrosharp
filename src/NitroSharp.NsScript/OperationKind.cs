namespace NitroSharp.NsScript
{
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

    public enum OperationCategory
    {
        None,
        Unary,
        Binary,
        Assignment
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
}
