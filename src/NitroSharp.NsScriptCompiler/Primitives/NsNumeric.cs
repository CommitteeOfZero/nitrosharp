namespace NitroSharp.NsScript
{
    public readonly struct NsNumeric
    {
        public static NsNumeric Zero = new NsNumeric(0, false);

        public NsNumeric(double value, bool isDelta)
        {
            Value = value;
            IsDelta = isDelta;
        }

        public double Value { get; }
        public bool IsDelta { get; }

        public void Assign(ref double target)
        {
            target = IsDelta ? target + Value : Value;
        }

        public void AssignTo(ref float target)
        {
            target = IsDelta ? target + (float)Value : (float)Value;
        }

        public static implicit operator double(NsNumeric numeric)
            => numeric.Value;

        public static implicit operator float(NsNumeric numeric)
            => (float)numeric.Value;

        public static explicit operator int(NsNumeric numeric)
            => (int)numeric.Value;

        public static NsNumeric operator *(NsNumeric a, double m)
            => new NsNumeric(a.Value * m, a.IsDelta);

        public static NsNumeric operator /(NsNumeric a, double m)
            => new NsNumeric(a.Value / m, a.IsDelta);
    }
}
