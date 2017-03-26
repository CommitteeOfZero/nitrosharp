namespace SciAdvNet.NSScript
{
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
