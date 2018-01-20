namespace NitroSharp.NsScript
{
    public enum UnaryOperatorKind
    {
        Not,
        Plus,
        Minus
    }

    public enum BinaryOperatorKind
    {
        Multiply,
        Divide,
        Remainder,
        Add,
        Subtract,
        Equals,
        NotEquals,
        LessThanOrEqual,
        GreaterThanOrEqual,
        LessThan,
        GreaterThan,
        And,
        Or
    }

    public enum AssignmentOperatorKind
    {
        Assign,
        AddAssign,
        SubtractAssign,
        MultiplyAssign,
        DivideAssign,
        Increment,
        Decrement
    }
}
