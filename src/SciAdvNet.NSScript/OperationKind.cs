namespace SciAdvNet.NSScript
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
}
