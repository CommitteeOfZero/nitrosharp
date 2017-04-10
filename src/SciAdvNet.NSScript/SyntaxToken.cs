namespace SciAdvNet.NSScript
{
    public sealed class SyntaxToken
    {
        internal SyntaxToken(SyntaxTokenKind kind, string leadingTrivia, string text, string trailingTrivia, object value)
        {
            Kind = kind;
            LeadingTrivia = leadingTrivia;
            Text = text;
            TrailingTrivia = trailingTrivia;
            Value = value;
        }

        internal SyntaxToken(SyntaxTokenKind kind, string leadingTrivia, string text, string trailingTrivia)
            : this(kind, leadingTrivia, text, trailingTrivia, null)
        {
        }


        public SyntaxTokenKind Kind { get; }
        public string LeadingTrivia { get; }
        public string Text { get; }
        public string TrailingTrivia { get; }
        public object Value { get; }

        public override string ToString()
        {
            return Text;
        }
    }
}
