using System;

namespace SciAdvNet.NSScript
{
    public struct Rational
    {
        public Rational(float numerator, float denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        public float Numerator { get; }
        public float Denominator { get; }

        public Rational Rebase(float newBase)
        {
            float newNumerator = Numerator * newBase / Denominator;
            return new Rational(newNumerator, newBase);
        }

        public static implicit operator float(Rational rational) => rational.Numerator / rational.Denominator;

        public override string ToString()
        {
            return $"{Numerator} / {Denominator}";
        }
    }
}
