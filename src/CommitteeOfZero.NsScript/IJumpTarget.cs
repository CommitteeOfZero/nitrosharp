namespace CommitteeOfZero.NsScript
{
    public interface IJumpTarget
    {
        Identifier Name { get; }
        Block Body { get; }
    }
}
