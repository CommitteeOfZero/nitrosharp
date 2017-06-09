namespace CommitteeOfZero.NsScript
{
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
        AtToken,

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

        ParagraphStartTag,
        ParagraphEndTag,
        ParagraphIdentifier,
        PXmlString,

        EndOfFileToken
    }
}
