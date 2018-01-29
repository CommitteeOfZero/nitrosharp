using NitroSharp.NsScript.Text;

namespace NitroSharp.NsScript.Syntax
{
    public class SyntaxToken
    {
        internal static SyntaxToken WithValue(SyntaxTokenKind kind, string text, TextSpan span, string value)
        {
            return new SyntaxTokenWithStringValue(kind, text, span, value);
        }

        internal static SyntaxToken Identifier(string text, TextSpan span, SigilKind sigil, bool isQuoted)
        {
            return new IdentifierToken(text, span, sigil, isQuoted);
        }

        internal static SyntaxToken Literal(string text, TextSpan span)
        {
            return new SyntaxTokenWithText(SyntaxTokenKind.StringLiteralToken, text, span);
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

    internal class SyntaxTokenWithText : SyntaxToken
    {
        internal SyntaxTokenWithText(SyntaxTokenKind kind, string text, TextSpan span) : base(kind, span)
        {
            Text = text;
        }

        public override string Text { get; }
    }

    internal sealed class SyntaxTokenWithStringValue : SyntaxTokenWithText
    {
        internal SyntaxTokenWithStringValue(SyntaxTokenKind kind, string text, TextSpan textSpan, string value)
            : base(kind, text, textSpan)
        {
            StringValue = value;
        }

        public string StringValue { get; }
        public override object Value => StringValue;
    }

    internal sealed class SyntaxTokenWithDoubleValue : SyntaxTokenWithText
    {
        internal SyntaxTokenWithDoubleValue(string text, TextSpan textSpan, double value)
            : base(SyntaxTokenKind.NumericLiteralToken, text, textSpan)
        {
            DoubleValue = value;
        }

        public double DoubleValue { get; }
        public override object Value => DoubleValue;
    }

    internal sealed class IdentifierToken : SyntaxTokenWithText
    {
        internal IdentifierToken(string text, TextSpan textSpan, SigilKind sigil, bool isQuoted)
            : base(SyntaxTokenKind.IdentifierToken, text, textSpan)
        {
            Sigil = sigil;
            IsQuoted = isQuoted;
        }

        public bool IsQuoted { get; }
        public SigilKind Sigil { get; }
    }
}
