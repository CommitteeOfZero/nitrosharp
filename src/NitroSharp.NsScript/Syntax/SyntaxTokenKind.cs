namespace NitroSharp.NsScript.Syntax
{
    public enum SyntaxTokenKind
    {
        None,
        BadToken,

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
        AtToken,
        HashToken,
        PercentToken,
        ArrowToken,
        AtArrowToken,

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
        IncludeKeyword,
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
