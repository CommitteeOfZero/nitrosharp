using NitroSharp.NsScript.Text;

namespace NitroSharp.NsScript.Syntax
{
    public class SyntaxToken
    {
        internal static SyntaxToken WithText(SyntaxTokenKind kind, string text, TextSpan span, Diagnostic syntaxError)
        {
            return new SyntaxTokenWithText(kind, text, span, syntaxError);
        }

        internal static SyntaxToken WithValue(SyntaxTokenKind kind, string value, TextSpan span, Diagnostic syntaxError)
        {
            return new SyntaxTokenWithStringValue(kind, value, span, syntaxError);
        }
        
        internal static SyntaxToken WithTextAndValue(SyntaxTokenKind kind, string text, TextSpan span, string value, Diagnostic syntaxError)
        {
            return new SyntaxTokenWithTextAndValue(kind, text, span, value, syntaxError);
        }

        internal static SyntaxToken Identifier(string text, TextSpan span, SigilKind sigil, bool isQuoted, Diagnostic syntaxError)
        {
            return new IdentifierToken(text, span, sigil, isQuoted, syntaxError);
        }

        internal static SyntaxToken Literal(string value, TextSpan span, Diagnostic syntaxError)
        {
            return new StringLiteralToken(value, span, syntaxError);
        }

        internal static SyntaxToken Literal(double value, TextSpan span, Diagnostic syntaxError)
        {
            return new NumericLiteralToken(value, span, syntaxError);
        }

        internal static SyntaxToken HexTriplet(double value, TextSpan span, Diagnostic syntaxError)
        {
            return new HexTripletToken(value, span, syntaxError);
        }

        internal static SyntaxToken DialogueBlockStartTag(string boxName, TextSpan span, Diagnostic syntaxError)
        {
            return new DialogueBlockStartTagToken(boxName, span, syntaxError);
        }

        internal static SyntaxToken DialogueBlockIdentifier(string name, TextSpan span, Diagnostic syntaxError)
        {
            return new DialogueBlockIdentifierToken(name, span, syntaxError);
        }

        internal SyntaxToken(SyntaxTokenKind kind, TextSpan span, Diagnostic syntaxError = null)
        {
            Kind = kind;
            TextSpan = span;
            SyntaxError = syntaxError;
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

        public Diagnostic SyntaxError { get; }
        public bool HasErrors => SyntaxError != null;

        public override string ToString()
        {
            return Text;
        }
    }

    internal class SyntaxTokenWithText : SyntaxToken
    {
        internal SyntaxTokenWithText(SyntaxTokenKind kind, string text, TextSpan span, Diagnostic syntaxError)
            : base(kind, span, syntaxError)
        {
            Text = text;
        }

        public override string Text { get; }
    }

    internal class SyntaxTokenWithStringValue : SyntaxToken
    {
        internal SyntaxTokenWithStringValue(SyntaxTokenKind kind, string value, TextSpan textSpan, Diagnostic syntaxError)
            : base(kind, textSpan, syntaxError)
        {
            StringValue = value;
        }

        public string StringValue { get; }
        public override object Value => StringValue;
    }

    internal sealed class SyntaxTokenWithTextAndValue : SyntaxTokenWithStringValue
    {
        internal SyntaxTokenWithTextAndValue(SyntaxTokenKind kind, string text, TextSpan textSpan, string value, Diagnostic syntaxError)
            : base(kind, value, textSpan, syntaxError)
        {
            Text = text;
        }

        public override string Text { get; }
    }
    
    internal sealed class StringLiteralToken : SyntaxTokenWithStringValue
    {
        internal StringLiteralToken(string value, TextSpan span, Diagnostic syntaxError)
            : base(SyntaxTokenKind.StringLiteralToken, value, span, syntaxError) { }

        public override string Text => "\"" + StringValue + "\"";
    }

    internal class NumericLiteralToken : SyntaxToken
    {
        internal NumericLiteralToken(double value, TextSpan textSpan, Diagnostic syntaxError)
            : base(SyntaxTokenKind.NumericLiteralToken, textSpan, syntaxError)
        {
            DoubleValue = value;
        }

        public double DoubleValue { get; }
        public override object Value => DoubleValue;

        public override string Text => DoubleValue.ToString();
    }

    internal sealed class HexTripletToken : NumericLiteralToken
    {
        internal HexTripletToken(double value, TextSpan textSpan, Diagnostic syntaxError) : base(value, textSpan, syntaxError)
        {
        }

        public int IntegralValue => (int)DoubleValue;
        public override string Text => "#" + IntegralValue.ToString("X");
    }

    internal sealed class IdentifierToken : SyntaxTokenWithStringValue
    {
        internal IdentifierToken(string name, TextSpan textSpan, SigilKind sigil, bool isQuoted, Diagnostic syntaxError)
            : base(SyntaxTokenKind.IdentifierToken, name, textSpan, syntaxError)
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
        internal DialogueBlockStartTagToken(string boxName, TextSpan textSpan, Diagnostic syntaxError)
        : base(SyntaxTokenKind.DialogueBlockStartTag, boxName, textSpan, syntaxError)
        {
        }

        public string BoxName => StringValue;
        public override string Text => "<PRE " + BoxName + ">";
    }

    internal sealed class DialogueBlockIdentifierToken : SyntaxTokenWithStringValue
    {
        internal DialogueBlockIdentifierToken(string name, TextSpan textSpan, Diagnostic syntaxError)
        : base(SyntaxTokenKind.DialogueBlockIdentifier, name, textSpan, syntaxError)
        {
        }

        public string Name => StringValue;
        public override string Text => "[" + Name + "]";
    }
}
