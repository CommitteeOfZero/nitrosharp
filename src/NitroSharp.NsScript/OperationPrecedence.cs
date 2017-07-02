namespace NitroSharp.NsScript
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
