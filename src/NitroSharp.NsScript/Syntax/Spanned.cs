using NitroSharp.NsScript.Text;

namespace NitroSharp.NsScript.Syntax
{
    public readonly struct Spanned<T>
    {
        internal Spanned(T value, TextSpan span)
        {
            Value = value;
            Span = span;
        }

        public T Value { get; }
        public TextSpan Span { get; }

        public override string ToString() => $"{Value!.ToString()} {Span.ToString()}";
    }
}
