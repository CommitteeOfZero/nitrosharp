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
        internal SyntaxTokenWithStringValue(SyntaxTokenKind kind, string text, TextSpan textSpan, string value)
            : base(kind, textSpan)
        {
            Text = text;
            StringValue = value;
        }

        public override string Text { get; }
        public string StringValue { get; }
        public override object Value => StringValue;
    }

    internal sealed class SyntaxTokenWithDoubleValue : SyntaxToken
    {
        internal SyntaxTokenWithDoubleValue(string text, TextSpan textSpan, double value)
            : base(SyntaxTokenKind.NumericLiteralToken, textSpan)
        {
            Text = text;
            DoubleValue = value;
        }

        public override string Text { get; }
        public double DoubleValue { get; }
        public override object Value => DoubleValue;
    }

    internal sealed class IdentifierToken : SyntaxTokenWithStringValue
    {
        internal IdentifierToken(string text, TextSpan textSpan, string nameWithoutSigil, SigilKind sigilCharacter)
            : base(SyntaxTokenKind.IdentifierToken, text, textSpan, nameWithoutSigil)
        {
            SigilCharacter = sigilCharacter;
        }

        public string NameWithoutSigil => StringValue;
        public SigilKind SigilCharacter { get; }
    }
}
