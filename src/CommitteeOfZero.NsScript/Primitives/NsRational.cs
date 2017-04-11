using System;

namespace CommitteeOfZero.NsScript
{
    public struct NsRational
    {
        public NsRational(float numerator, float denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        public float Numerator { get; }
        public float Denominator { get; }

        public NsRational Rebase(float newBase)
        {
            float newNumerator = Numerator * newBase / Denominator;
            return new NsRational(newNumerator, newBase);
        }

        public static implicit operator float(NsRational rational) => rational.Numerator / rational.Denominator;

        public override string ToString()
        {
            return $"{Numerator} / {Denominator}";
        }
    }
}
