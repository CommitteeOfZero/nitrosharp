using NitroSharp.NsScript.Text;

namespace NitroSharp.NsScript.Syntax
{
    public class SyntaxToken
    {
        internal static SyntaxToken WithValue(SyntaxTokenKind kind, string text, TextSpan span, string value)
        {
            return new SyntaxTokenWithStringValue(kind, text, span, value);
        }

        internal static SyntaxToken Identifier(string text, TextSpan span, string nameWithoutSigil, SigilKind sigilCharacter)
        {
            return new IdentifierToken(text, span, nameWithoutSigil, sigilCharacter);
        }

        internal static SyntaxToken Literal(string text, TextSpan span, string value)
        {
            return WithValue(SyntaxTokenKind.StringLiteralToken, text, span, value);
        }

        internal static SyntaxToken Literal(string text, TextSpan span, double value)
        {
            return new SyntaxTokenWithDoubleValue(text, span, value);
        }

        internal SyntaxToken(SyntaxTokenKind kind, TextSpan span)
        {
            Kind = kind;
            TextSpan = span;
        }

        public SyntaxTokenKind Kind { get; }
        public virtual string Text => SyntaxFacts.GetText(Kind);
        public TextSpan TextSpan { get; }
        public virtual object Value
        {
            get
            {
                switch (Kind)
                {
                    case SyntaxTokenKind.TrueKeyword:
                        return true;
                    case SyntaxTokenKind.FalseKeyword:
                        return false;
                    case SyntaxTokenKind.NullKeyword:
                        return null;
                    default:
                        return Text;
                }
            }
        }

        public override string ToString()
        {
            return Text;
        }
    }

    internal class SyntaxTokenWithStringValue : SyntaxToken
    {
        protected readonly string _text;
        protected readonly string _value;

        internal SyntaxTokenWithStringValue(SyntaxTokenKind kind, string text, TextSpan textSpan, string value)
            : base(kind, textSpan)
        {
            _text = text;
            _value = value;
        }

        public override string Text => _text;
        public override object Value => _value;
    }

    internal sealed class SyntaxTokenWithDoubleValue : SyntaxToken
    {
        private readonly string _text;
        private readonly double _value;

        internal SyntaxTokenWithDoubleValue(string text, TextSpan textSpan, double value)
            : base(SyntaxTokenKind.NumericLiteralToken, textSpan)
        {
            _text = text;
            _value = value;
        }

        public override string Text => _text;
        public override object Value => _value;
    }

    internal sealed class IdentifierToken : SyntaxTokenWithStringValue
    {
        internal IdentifierToken(string text, TextSpan textSpan, string nameWithoutSigil, SigilKind sigilCharacter)
            : base(SyntaxTokenKind.IdentifierToken, text, textSpan, nameWithoutSigil)
        {
            SigilCharacter = sigilCharacter;
        }

        public string NameWithoutSigil => _value;
        public SigilKind SigilCharacter { get; }
    }
}
