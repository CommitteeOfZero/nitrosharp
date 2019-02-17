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
        {
            return numeric.Value;
        }

        public static implicit operator float(NsNumeric numeric)
        {
            return (float)numeric.Value;
        }

        public static explicit operator int(NsNumeric numeric)
        {
            return (int)numeric.Value;
        }

        public static NsNumeric operator *(NsNumeric a, double m)
        {
            return new NsNumeric(a.Value * m, a.IsDelta);
        }

        public static NsNumeric operator /(NsNumeric a, double m)
        {
            return new NsNumeric(a.Value / m, a.IsDelta);
        }
    }
}
