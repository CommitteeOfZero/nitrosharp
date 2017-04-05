using System;

namespace SciAdvNet.NSScript
{
    public struct Rational
    {
        public Rational(int numerator, int denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        public int Numerator { get; }
        public int Denominator { get; }

        public Rational Rebase(int newBase)
        {
            int newNumerator = (int)Math.Round((float)Numerator * newBase / Denominator);
            return new Rational(newNumerator, newBase);
        }

        public static implicit operator float(Rational rational) => (float)rational.Numerator / rational.Denominator;
    }
}
