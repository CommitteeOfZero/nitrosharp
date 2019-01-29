using NitroSharp.NsScriptNew.Text;

namespace NitroSharp.NsScriptNew.Syntax
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
