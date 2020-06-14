using NitroSharp.NsScript;
using NitroSharp.NsScript.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NitroSharp.NsScriptCompiler.Tests
{
    public class LexerTests
    {
        [Theory]
        [InlineData(SyntaxTokenKind.At, "@")]
        [InlineData(SyntaxTokenKind.Dollar, "$")]
        [InlineData(SyntaxTokenKind.Hash, "#")]
        public void Regression_Is_Fixed(SyntaxTokenKind kind, string text)
        {
            (SyntaxToken token, LexingContext ctx) = LexToken(text);
            Assert.Equal(kind, token.Kind);
            Assert.Equal(text, SyntaxFacts.GetText(kind));
            Assert.Equal(text, ctx.GetText(token).ToString(), ignoreCase: true);
            Assert.Equal(SyntaxTokenFlags.Empty, token.Flags);
        }

        [Theory]
        [InlineData("\"foo", DiagnosticId.UnterminatedString, 0, 0)]
        [InlineData("<PRE box00", DiagnosticId.UnterminatedDialogueBlockStartTag, 0, 0)]
        [InlineData("/* multiline comment", DiagnosticId.UnterminatedComment, 0, 0)]
        [InlineData("[text001", DiagnosticId.UnterminatedDialogueBlockIdentifier, 0, 0, LexingMode.DialogueBlock)]
        [InlineData("2147483648", DiagnosticId.NumberTooLarge, 0, 10)]
        public void Lexer_Emits_Diagnostics(
            string text, DiagnosticId diagnosticId,
            int spanStart, int spanEnd, LexingMode lexingMode = LexingMode.Normal)
        {
            (SyntaxToken token, LexingContext ctx) = LexToken(text, lexingMode);
            var diagnostic = Assert.Single(ctx.Diagnostics.All);
            Assert.Equal(diagnosticId, diagnostic.Id);
            Assert.Equal(TextSpan.FromBounds(spanStart, spanEnd), diagnostic.Span);
        }

        [Fact]
        public void All_Static_Tokens_Are_Tested()
        {
            var tokens = Enum.GetValues(typeof(SyntaxTokenKind))
                .Cast<SyntaxTokenKind>();

            var untestedTokens = new HashSet<SyntaxTokenKind>(tokens);
            untestedTokens.ExceptWith(GetDynamicTokens());
            untestedTokens.Remove(SyntaxTokenKind.None);
            untestedTokens.Remove(SyntaxTokenKind.BadToken);
            untestedTokens.Remove(SyntaxTokenKind.MissingToken);
            untestedTokens.Remove(SyntaxTokenKind.EndOfFileToken);

            var testedTokens = GetStaticTokens().Select(x => x.kind);
            untestedTokens.ExceptWith(testedTokens);

            Assert.Empty(untestedTokens);
        }

        [Theory]
        [MemberData(nameof(GetStaticTokenData))]
        public void StaticToken(SyntaxTokenKind kind, string text)
        {
            (SyntaxToken token, LexingContext ctx) = LexToken(text);
            Assert.Equal(kind, token.Kind);
            Assert.Equal(text, SyntaxFacts.GetText(kind));
            Assert.Equal(text, ctx.GetText(token).ToString(), ignoreCase: true);
            Assert.Equal(SyntaxTokenFlags.Empty, token.Flags);
        }

        [Theory]
        [MemberData(nameof(GetStaticTokenPairData))]
        public void TokenPair(SyntaxTokenKind t1Kind, string t1Text, SyntaxTokenKind t2Kind, string t2Text)
        {
            string text = t1Text + t2Text;
            (SyntaxTokenEnumerable tkEnumerable, LexingContext ctx) = Parsing.LexTokens(text);
            var tokens = tkEnumerable.ToArray();
            Assert.Equal(3, tokens.Length);
            Assert.Equal(tokens[0].Kind, t1Kind);
            Assert.Equal(ctx.GetText(tokens[0]).ToString(), t1Text, ignoreCase: true);
            Assert.Equal(tokens[1].Kind, t2Kind);
            Assert.Equal(ctx.GetText(tokens[1]).ToString(), t2Text, ignoreCase: true);
        }

        [Theory]
        [MemberData(nameof(GetStaticTokenPairsWithSeparatorData))]
        public void Token_Pair_With_Separator(
            SyntaxTokenKind t1Kind, string t1Text,
            string separator,
            SyntaxTokenKind t2Kind, string t2Text)
        {
            string text = t1Text + separator + t2Text;
            (SyntaxTokenEnumerable tkEnumerable, LexingContext ctx) = Parsing.LexTokens(text);
            var tokens = tkEnumerable.ToArray();
            Assert.Equal(3, tokens.Length);
            Assert.Equal(tokens[0].Kind, t1Kind);
            Assert.Equal(ctx.GetText(tokens[0]).ToString(), t1Text, ignoreCase: true);
            Assert.Equal(tokens[1].Kind, t2Kind);
            Assert.Equal(ctx.GetText(tokens[1]).ToString(), t2Text, ignoreCase: true);
        }

        [Theory]
        [InlineData("42", SyntaxTokenKind.NumericLiteral, "42")]
        [InlineData("42.2", SyntaxTokenKind.NumericLiteral, "42.2", SyntaxTokenFlags.HasDecimalPoint)]
        [InlineData("#FFFFFF", SyntaxTokenKind.NumericLiteral, "FFFFFF", SyntaxTokenFlags.IsHexTriplet)]
        [InlineData("\"foo\"", SyntaxTokenKind.StringLiteralOrQuotedIdentifier, "foo", SyntaxTokenFlags.IsQuoted)]
        [InlineData("\"@\"", SyntaxTokenKind.StringLiteralOrQuotedIdentifier, "@", SyntaxTokenFlags.IsQuoted)]
        public void Literal(string text, SyntaxTokenKind tokenKind, string valueText,
            SyntaxTokenFlags flags = SyntaxTokenFlags.Empty)
        {
            (SyntaxToken token, LexingContext ctx) = LexToken(text);
            Assert.Equal(tokenKind, token.Kind);
            Assert.Equal(text, ctx.GetText(token).ToString());
            Assert.Equal(valueText, ctx.GetValueText(token).ToString());
            Assert.Equal(flags, token.Flags);
        }

        [Theory]
        [InlineData("foo", "foo", SyntaxTokenFlags.Empty)]
        [InlineData("$foo", "foo", SyntaxTokenFlags.HasDollarPrefix)]
        [InlineData("#foo", "foo", SyntaxTokenFlags.HasHashPrefix)]
        [InlineData("@foo", "@foo", SyntaxTokenFlags.HasAtPrefix)]
        public void Identifier(string text, string valueText, SyntaxTokenFlags flags)
        {
            (SyntaxToken token, LexingContext ctx) = LexToken(text);
            Assert.Equal(SyntaxTokenKind.Identifier, token.Kind);
            Assert.Equal(text, ctx.GetText(token).ToString());
            Assert.Equal(valueText, ctx.GetValueText(token).ToString());
            Assert.Equal(flags, token.Flags);
        }

        [Theory]
        [InlineData("\"$foo\"", "foo", SyntaxTokenFlags.IsQuoted | SyntaxTokenFlags.HasDollarPrefix)]
        [InlineData("\"#foo\"", "foo", SyntaxTokenFlags.IsQuoted | SyntaxTokenFlags.HasHashPrefix)]
        [InlineData("\"@foo\"", "@foo", SyntaxTokenFlags.IsQuoted | SyntaxTokenFlags.HasAtPrefix)]
        public void StringLiteralOrQuotedIdentifier(string text, string valueText, SyntaxTokenFlags flags)
        {
            (SyntaxToken token, LexingContext ctx) = LexToken(text);
            Assert.Equal(SyntaxTokenKind.StringLiteralOrQuotedIdentifier, token.Kind);
            Assert.Equal(text, ctx.GetText(token).ToString());
            Assert.Equal(valueText, ctx.GetValueText(token).ToString());
            Assert.Equal(flags, token.Flags);
        }

        [Theory]
        [InlineData(SyntaxTokenKind.TrueKeyword)]
        [InlineData(SyntaxTokenKind.FalseKeyword)]
        [InlineData(SyntaxTokenKind.NullKeyword)]
        public void Recognizes_Certain_Quoted_Keywords(SyntaxTokenKind keyword)
        {
            string quotedText = $"\"{SyntaxFacts.GetText(keyword)}\"";
            (SyntaxToken token, LexingContext ctx) = LexToken(quotedText);
            Assert.Equal(keyword, token.Kind);
            Assert.Equal(SyntaxTokenFlags.IsQuoted, token.Flags);
        }

        [Theory]
        [InlineData("True", SyntaxTokenKind.TrueKeyword)]
        [InlineData("False", SyntaxTokenKind.FalseKeyword)]
        [InlineData("Null", SyntaxTokenKind.NullKeyword)]
        public void Recognizes_Certain_PascalCase_Keywords(string text, SyntaxTokenKind keyword)
        {
            (SyntaxToken token, LexingContext _) = LexToken(text);
            Assert.Equal(keyword, token.Kind);
        }

        [Theory]
        [InlineData("TRUE", SyntaxTokenKind.TrueKeyword)]
        [InlineData("FALSE", SyntaxTokenKind.FalseKeyword)]
        [InlineData("NULL", SyntaxTokenKind.NullKeyword)]
        public void Recognizes_Certain_UpperCase_Keywords(string text, SyntaxTokenKind keyword)
        {
            (SyntaxToken token, LexingContext _) = LexToken(text);
            Assert.Equal(keyword, token.Kind);
        }

        [Fact]
        public void Identifier_Cannot_Start_With_Dot()
        {
            (SyntaxTokenEnumerable tkEnumerable, LexingContext ctx) = Parsing.LexTokens("$.");
            var tokens = tkEnumerable.ToArray();
            Assert.Equal(3, tokens.Length);
            Assert.Equal(SyntaxTokenKind.Dollar, tokens[0].Kind);
            Assert.Equal(SyntaxTokenKind.Dot, tokens[1].Kind);
        }

        [Theory]
        [InlineData("[text001]", SyntaxTokenKind.DialogueBlockIdentifier)]
        [InlineData("\r", SyntaxTokenKind.PXmlLineSeparator)]
        [InlineData("\n", SyntaxTokenKind.PXmlLineSeparator)]
        [InlineData("foo", SyntaxTokenKind.PXmlString)]
        public void Dynamic_PXml_Token(string text, SyntaxTokenKind kind)
        {
            (SyntaxToken token, LexingContext ctx) = LexToken(text, LexingMode.DialogueBlock);
            Assert.Equal(kind, token.Kind);
            Assert.Equal(text, ctx.GetText(token).ToString());
            Assert.Equal(SyntaxTokenFlags.Empty, token.Flags);
        }

        [Theory]
        [InlineData("<PRE box00>", SyntaxTokenKind.DialogueBlockStartTag)]
        [InlineData("<pre box00>", SyntaxTokenKind.DialogueBlockStartTag)]
        [InlineData("</PRE>", SyntaxTokenKind.DialogueBlockEndTag)]
        [InlineData("</pre>", SyntaxTokenKind.DialogueBlockEndTag)]
        public void Dialogue_Block_Tag(string text, SyntaxTokenKind kind)
        {
            (SyntaxToken token, LexingContext ctx) = LexToken(text);
            Assert.Equal(kind, token.Kind);
            Assert.Equal(text, ctx.GetText(token).ToString());
            Assert.Equal(SyntaxTokenFlags.Empty, token.Flags);
        }

        private static (SyntaxToken token, LexingContext ctx) LexToken(string text, LexingMode mode = LexingMode.Normal)
        {
            SyntaxToken tk = default;
            (SyntaxTokenEnumerable tokens, LexingContext ctx) = Parsing.LexTokens(text, mode);
            foreach (SyntaxToken token in tokens)
            {
                if (tk.Kind == SyntaxTokenKind.None)
                {
                    tk = token;
                }
                else if (token.Kind != SyntaxTokenKind.EndOfFileToken)
                {
                    Assert.True(false, "More than one token was lexed.");
                }
            }

            if (tk.Kind == SyntaxTokenKind.None)
            {
                Assert.True(false, "No tokens were lexed.");
            }

            return (tk, ctx);
        }

        public static IEnumerable<object[]> GetStaticTokenData()
        {
            return from token in GetStaticTokens()
                   where token.kind != SyntaxTokenKind.PXmlLineSeparator
                   select new object[] { token.kind, token.text };
        }

        public static IEnumerable<object[]> GetStaticTokenPairData()
        {
            return GetStaticTokenPairs()
                .Select(pair => new object[] { pair.t1Kind, pair.t1Text, pair.t2Kind, pair.t2Text });
        }

        public static IEnumerable<object[]> GetStaticTokenPairsWithSeparatorData()
        {
            return GetStaticTokenPairsWithSeparator()
                .Select(pair => new object[] { pair.t1Kind, pair.t1Text, pair.separatorText, pair.t2Kind, pair.t2Text });
        }

        private static IEnumerable<(SyntaxTokenKind kind, string text)> GetStaticTokens()
        {
            var fixedTokens = Enum.GetValues(typeof(SyntaxTokenKind))
                                  .Cast<SyntaxTokenKind>()
                                  .Select(k => (kind: k, text: SyntaxFacts.GetText(k)))
                                  .Where(t => !string.IsNullOrEmpty(t.text));

            return fixedTokens;
        }

        private static SyntaxTokenKind[] GetDynamicTokens()
        {
            return new[]
            {
                SyntaxTokenKind.Identifier,
                SyntaxTokenKind.NumericLiteral,
                SyntaxTokenKind.StringLiteralOrQuotedIdentifier,
                SyntaxTokenKind.DialogueBlockStartTag,
                SyntaxTokenKind.DialogueBlockIdentifier,
                SyntaxTokenKind.DialogueBlockEndTag,
                SyntaxTokenKind.PXmlString
            };
        }

        private static IEnumerable<(SyntaxTokenKind t1Kind, string t1Text, SyntaxTokenKind t2Kind, string t2Text)> GetStaticTokenPairs()
        {
            return from tk1 in GetStaticTokens()
                   where tk1.kind != SyntaxTokenKind.PXmlLineSeparator
                   from tk2 in GetStaticTokens()
                   where tk2.kind != SyntaxTokenKind.PXmlLineSeparator
                   where !RequireSeparator(tk1.kind, tk2.kind)
                   select (tk1.kind, tk1.text, tk2.kind, tk2.text);
        }

        private static IEnumerable<(SyntaxTokenKind t1Kind, string t1Text,
                                    string separatorText,
                                    SyntaxTokenKind t2Kind, string t2Text)> GetStaticTokenPairsWithSeparator()
        {
            return from tk1 in GetStaticTokens()
                   where tk1.kind != SyntaxTokenKind.PXmlLineSeparator
                   from tk2 in GetStaticTokens()
                   where tk2.kind != SyntaxTokenKind.PXmlLineSeparator
                   where RequireSeparator(tk1.kind, tk2.kind)
                   from separator in GetSeparators()
                   select (tk1.kind, tk1.text, separator, tk2.kind, tk2.text);
        }

        private static string[] GetSeparators()
        {
            return new[]
            {
                " ",
                "\t",
                "\r",
                "\n",
                "\r\n",
            };
        }

        private static bool RequireSeparator(SyntaxTokenKind kind1, SyntaxTokenKind kind2)
        {
            static bool isKeyword(SyntaxTokenKind kind) =>
                (int)kind >= (int)SyntaxTokenKind.ChapterKeyword
                && (int)kind <= (int)SyntaxTokenKind.ReturnKeyword;

            bool isIdentifierOrKeyword(SyntaxTokenKind kind) =>
                isKeyword(kind)
                || kind == SyntaxTokenKind.Identifier;

            bool canFollowKeyword(SyntaxTokenKind kind)
                => !isKeyword(kind)
                && SyntaxFacts.IsIdentifierStopCharacter(SyntaxFacts.GetText(kind)[0], char.MaxValue);

            static bool isSigil(SyntaxTokenKind kind)
                => kind == SyntaxTokenKind.Dollar
                || kind == SyntaxTokenKind.Hash
                || kind == SyntaxTokenKind.At;

            bool canFormCompountPunctuation(SyntaxTokenKind kind)
            {
                switch (kind1)
                {
                    case SyntaxTokenKind.Equals:
                    case SyntaxTokenKind.Minus:
                    case SyntaxTokenKind.Plus:
                    case SyntaxTokenKind.Asterisk:
                    case SyntaxTokenKind.Slash:
                    case SyntaxTokenKind.LessThan:
                    case SyntaxTokenKind.GreaterThan:
                    case SyntaxTokenKind.Exclamation:
                    case SyntaxTokenKind.Ampersand:
                        return true;

                    default:
                        return false;
                }
            }

            if (isIdentifierOrKeyword(kind1) && isSigil(kind2)) return true;
            if (isIdentifierOrKeyword(kind2) && isSigil(kind1)) return true;

            bool tk1IsKeyword = isKeyword(kind1);
            bool tk2IsKeyword = isKeyword(kind2);

            if (tk1IsKeyword)
            {
                return !canFollowKeyword(kind2);
            }
            if (tk2IsKeyword)
            {
                return !canFollowKeyword(kind1);
            }

            if (canFormCompountPunctuation(kind1) && canFormCompountPunctuation(kind2))
            {
                return true;
            }

            // <dot><dot>
            if (kind1 == SyntaxTokenKind.Dot && kind2 == SyntaxTokenKind.Dot)
            {
                return true;
            }

            // @->
            if (kind1 == SyntaxTokenKind.At && kind2 == SyntaxTokenKind.Arrow)
            {
                return true;
            }

            return false;
        }

        public static IEnumerable<object[]> GetKeywordData()
        {
            return EnumerateKeywords()
                .Select(kind => new object[] { kind, SyntaxFacts.GetText(kind) });
        }

        private static IEnumerable<SyntaxTokenKind> EnumerateKeywords()
        {
            const int start = (int)SyntaxTokenKind.ChapterKeyword;
            const int end = (int)SyntaxTokenKind.ReturnKeyword;
            for (int kind = start; kind <= end; kind *= 2)
            {
                yield return (SyntaxTokenKind)kind;
            }
        }
    }
}
