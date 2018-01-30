using NitroSharp.NsScript.Text;

namespace NitroSharp.NsScript.Syntax
{
    public class SyntaxToken
    {
        internal static SyntaxToken WithText(SyntaxTokenKind kind, string text, TextSpan span)
        {
            return new SyntaxTokenWithText(kind, text, span);
        }
        
        internal static SyntaxToken WithTextAndValue(SyntaxTokenKind kind, string text, TextSpan span, string value)
        {
            return new SyntaxTokenWithTextAndValue(kind, text, span, value);
        }

        internal static SyntaxToken Identifier(string text, TextSpan span, SigilKind sigil, bool isQuoted)
        {
            return new IdentifierToken(text, span, sigil, isQuoted);
        }

        internal static SyntaxToken Literal(string value, TextSpan span)
        {
            return new StringLiteralToken(value, span);
        }

        internal static SyntaxToken Literal(string text, TextSpan span, double value)
        {
            return new NumericLiteralToken(text, span, value);
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

    internal class SyntaxTokenWithStringValue : SyntaxToken
    {
        internal SyntaxTokenWithStringValue(SyntaxTokenKind kind, string value, TextSpan textSpan)
            : base(kind, textSpan)
        {
            StringValue = value;
        }

        public string StringValue { get; }
        public override object Value => StringValue;
    }

    internal sealed class SyntaxTokenWithTextAndValue : SyntaxTokenWithStringValue
    {
        internal SyntaxTokenWithTextAndValue(SyntaxTokenKind kind, string text, TextSpan textSpan, string value)
            : base(kind, value, textSpan)
        {
            Text = text;
        }

        public override string Text { get; }
    }
    
    internal sealed class StringLiteralToken : SyntaxTokenWithStringValue
    {
        internal StringLiteralToken(string value, TextSpan span) : base(SyntaxTokenKind.StringLiteralToken, value, span) { }

        public override string Text => "\"" + StringValue + "\"";
    }

    internal sealed class NumericLiteralToken : SyntaxTokenWithText
    {
        internal NumericLiteralToken(string text, TextSpan textSpan, double value)
            : base(SyntaxTokenKind.NumericLiteralToken, text, textSpan)
        {
            DoubleValue = value;
        }

        public double DoubleValue { get; }
        public override object Value => DoubleValue;
    }

    internal sealed class IdentifierToken : SyntaxTokenWithStringValue
    {
        internal IdentifierToken(string name, TextSpan textSpan, SigilKind sigil, bool isQuoted)
            : base(SyntaxTokenKind.IdentifierToken, name, textSpan)
        {
            Sigil = sigil;
            IsQuoted = isQuoted;
        }

        public bool IsQuoted { get; }
        public SigilKind Sigil { get; }

        public override string Text
        {
            get
            {
                string s = SyntaxFacts.GetText(Sigil) + StringValue;
                if (IsQuoted)
                {
                    s = "\"" + s + "\"";
                }

                return s;
            }
        }
    }
}
