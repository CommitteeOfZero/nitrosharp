namespace NitroSharp.NsScript;

public readonly struct NsNumeric(float value, bool isDelta)
{
    public static NsNumeric Zero => new(0, false);

    public float Value { get; } = value;
    public bool IsDelta { get; } = isDelta;

    public void Assign(ref double target)
    {
        target = IsDelta ? target + Value : Value;
    }

    public void AssignTo(ref float target)
    {
        target = IsDelta ? target + Value : Value;
    }

    public static implicit operator double(NsNumeric numeric)
        => numeric.Value;

    public static implicit operator float(NsNumeric numeric)
        => numeric.Value;

    public static explicit operator int(NsNumeric numeric)
        => (int)numeric.Value;

    public static NsNumeric operator *(NsNumeric a, float m)
        => new(a.Value * m, a.IsDelta);

    public static NsNumeric operator /(NsNumeric a, float m)
        => new(a.Value / m, a.IsDelta);
}
