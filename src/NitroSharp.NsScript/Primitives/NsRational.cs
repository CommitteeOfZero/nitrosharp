namespace NitroSharp.NsScript;

public readonly struct NsRational(float numerator, float denominator)
{
    public float Numerator { get; } = numerator;
    public float Denominator { get; } = denominator;

    public NsRational Rebase(float newBase)
    {
        float newNumerator = Numerator * newBase / Denominator;
        return new NsRational(newNumerator, newBase);
    }

    public static implicit operator float(NsRational rational)
        => rational.Numerator / rational.Denominator;

    public override string ToString() => $"{Numerator} / {Denominator}";
}
