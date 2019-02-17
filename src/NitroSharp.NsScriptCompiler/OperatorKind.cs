namespace NitroSharp.NsScript
{
    public enum UnaryOperatorKind : byte
    {
        Not = 0,
        Plus,
        Minus,
        Delta
    }

    public enum BinaryOperatorKind : byte
    {
        Multiply = 0,
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

    public enum AssignmentOperatorKind : byte
    {
        Assign = 0,
        AddAssign,
        SubtractAssign,
        MultiplyAssign,
        DivideAssign,
        Increment,
        Decrement
    }
}
