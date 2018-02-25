using NitroSharp.NsScript.Text;

namespace NitroSharp.NsScript.Syntax
{
    public class SyntaxToken
    {
        internal static SyntaxToken WithText(SyntaxTokenKind kind, string text, TextSpan span, Diagnostic diagnostic)
        {
            return new SyntaxTokenWithText(kind, text, span, diagnostic);
        }

        internal static SyntaxToken WithValue(SyntaxTokenKind kind, string value, TextSpan span, Diagnostic diagnostic)
        {
            return new SyntaxTokenWithStringValue(kind, value, span, diagnostic);
        }
        
        internal static SyntaxToken WithTextAndValue(SyntaxTokenKind kind, string text, TextSpan span, string value, Diagnostic diagnostic)
        {
            return new SyntaxTokenWithTextAndValue(kind, text, span, value, diagnostic);
        }

        internal static SyntaxToken Identifier(string text, TextSpan span, SigilKind sigil, bool isQuoted, Diagnostic diagnostic)
        {
            return new IdentifierToken(text, span, sigil, isQuoted, diagnostic);
        }

        internal static SyntaxToken Literal(string value, TextSpan span, Diagnostic diagnostic)
        {
            return new StringLiteralToken(value, span, diagnostic);
        }

        internal static SyntaxToken Literal(double value, TextSpan span, Diagnostic diagnostic)
        {
            return new NumericLiteralToken(value, span, diagnostic);
        }

        internal static SyntaxToken HexTriplet(double value, TextSpan span, Diagnostic diagnostic)
        {
            return new HexTripletToken(value, span, diagnostic);
        }

        internal static SyntaxToken DialogueBlockStartTag(string boxName, TextSpan span, Diagnostic diagnostic)
        {
            return new DialogueBlockStartTagToken(boxName, span, diagnostic);
        }

        internal static SyntaxToken DialogueBlockIdentifier(string name, TextSpan span, Diagnostic diagnostic)
        {
            return new DialogueBlockIdentifierToken(name, span, diagnostic);
        }

        internal static SyntaxToken Missing(SyntaxTokenKind kind, TextSpan span)
        {
            return WithText(SyntaxTokenKind.MissingToken, SyntaxFacts.GetText(kind), span, null);
        }

        internal SyntaxToken(SyntaxTokenKind kind, TextSpan span, Diagnostic diagnostic = null)
        {
            Kind = kind;
            Span = span;
            Diagnostic = diagnostic;
        }

        public SyntaxTokenKind Kind { get; }
        public virtual string Text => SyntaxFacts.GetText(Kind);
        public TextSpan Span { get; }
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

        public Diagnostic Diagnostic { get; }
        public bool HasDiagnostics => Diagnostic != null;

        public override string ToString()
        {
            return Text;
        }
    }

    internal class SyntaxTokenWithText : SyntaxToken
    {
        internal SyntaxTokenWithText(SyntaxTokenKind kind, string text, TextSpan span, Diagnostic diagnostic)
            : base(kind, span, diagnostic)
        {
            Text = text;
        }

        public override string Text { get; }
    }

    internal class SyntaxTokenWithStringValue : SyntaxToken
    {
        internal SyntaxTokenWithStringValue(SyntaxTokenKind kind, string value, TextSpan textSpan, Diagnostic diagnostic)
            : base(kind, textSpan, diagnostic)
        {
            StringValue = value;
        }

        public string StringValue { get; }
        public override object Value => StringValue;
    }

    internal sealed class SyntaxTokenWithTextAndValue : SyntaxTokenWithStringValue
    {
        internal SyntaxTokenWithTextAndValue(SyntaxTokenKind kind, string text, TextSpan textSpan, string value, Diagnostic diagnostic)
            : base(kind, value, textSpan, diagnostic)
        {
            Text = text;
        }

        public override string Text { get; }
    }
    
    internal sealed class StringLiteralToken : SyntaxTokenWithStringValue
    {
        internal StringLiteralToken(string value, TextSpan span, Diagnostic diagnostic)
            : base(SyntaxTokenKind.StringLiteralToken, value, span, diagnostic) { }

        public override string Text => "\"" + StringValue + "\"";
    }

    internal class NumericLiteralToken : SyntaxToken
    {
        internal NumericLiteralToken(double value, TextSpan textSpan, Diagnostic diagnostic)
            : base(SyntaxTokenKind.NumericLiteralToken, textSpan, diagnostic)
        {
            DoubleValue = value;
        }

        public double DoubleValue { get; }
        public override object Value => DoubleValue;

        public override string Text => DoubleValue.ToString();
    }

    internal sealed class HexTripletToken : NumericLiteralToken
    {
        internal HexTripletToken(double value, TextSpan textSpan, Diagnostic diagnostic) : base(value, textSpan, diagnostic)
        {
        }

        public int IntegralValue => (int)DoubleValue;
        public override string Text => "#" + IntegralValue.ToString("X");
    }

    internal sealed class IdentifierToken : SyntaxTokenWithStringValue
    {
        internal IdentifierToken(string name, TextSpan textSpan, SigilKind sigil, bool isQuoted, Diagnostic diagnostic)
            : base(SyntaxTokenKind.IdentifierToken, name, textSpan, diagnostic)
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

    internal sealed class DialogueBlockStartTagToken : SyntaxTokenWithStringValue
    {
        internal DialogueBlockStartTagToken(string boxName, TextSpan textSpan, Diagnostic diagnostic)
            : base(SyntaxTokenKind.DialogueBlockStartTag, boxName, textSpan, diagnostic)
        {
        }

        public string BoxName => StringValue;
        public override string Text => "<PRE " + BoxName + ">";
    }

    internal sealed class DialogueBlockIdentifierToken : SyntaxTokenWithStringValue
    {
        internal DialogueBlockIdentifierToken(string name, TextSpan textSpan, Diagnostic diagnostic)
            : base(SyntaxTokenKind.DialogueBlockIdentifier, name, textSpan, diagnostic)
        {
        }

        public string Name => StringValue;
        public override string Text => "[" + Name + "]";
    }
}
