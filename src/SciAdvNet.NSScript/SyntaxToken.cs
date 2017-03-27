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

    public enum SyntaxTokenKind
    {
        None,
        IncludeDirective,

        IdentifierToken,
        NumericLiteralToken,
        StringLiteralToken,

        // Punctuation
        OpenBraceToken,
        CloseBraceToken,
        OpenParenToken,
        CloseParenToken,
        CommaToken,
        DotToken,
        ColonToken,
        SemicolonToken,
        EqualsToken,
        PlusToken,
        MinusToken,
        AsteriskToken,
        SlashToken,
        LessThanToken,
        GreaterThanToken,
        ExclamationToken,
        AmpersandToken,

        // Compound punctuation
        EqualsEqualsToken,
        PlusPlusToken,
        MinusMinusToken,
        PlusEqualsToken,
        MinusEqualsToken,
        AsteriskEqualsToken,
        SlashEqualsToken,
        LessThanEqualsToken,
        GreaterThanEqualsToken,
        ExclamationEqualsToken,
        BarBarToken,
        AmpersandAmpersandToken,

        // Keywords
        ChapterKeyword,
        FunctionKeyword,
        SceneKeyword,
        CallSceneKeyword,
        CallChapterKeyword,
        NullKeyword,
        TrueKeyword,
        FalseKeyword,
        WhileKeyword,
        IfKeyword,
        ElseKeyword,
        SelectKeyword,
        CaseKeyword,
        BreakKeyword,
        ReturnKeyword,

        DialogueBlockStartTag,
        DialogueBlockEndTag,
        DialogueBlockIdentifier,
        PXmlString,
        PXmlLineSeparator,

        EndOfFileToken
    }
}
