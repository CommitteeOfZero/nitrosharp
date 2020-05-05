namespace NitroSharp.NsScript
{
    public readonly struct NsNumeric
    {
        public static NsNumeric Zero => new NsNumeric(0, false);

        public NsNumeric(float value, bool isDelta)
        {
            Value = value;
            IsDelta = isDelta;
        }

        public float Value { get; }
        public bool IsDelta { get; }

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
            => new NsNumeric(a.Value * m, a.IsDelta);

        public static NsNumeric operator /(NsNumeric a, float m)
            => new NsNumeric(a.Value / m, a.IsDelta);
    }
}
