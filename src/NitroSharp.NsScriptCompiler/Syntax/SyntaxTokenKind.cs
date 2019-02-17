namespace NitroSharp.NsScript.Syntax
{
    public enum SyntaxTokenKind : byte
    {
        None,
        BadToken,
        MissingToken,

        IncludeDirective,

        Identifier,
        NumericLiteral,
        StringLiteralOrQuotedIdentifier,

        // Punctuation
        OpenBrace,
        CloseBrace,
        OpenParen,
        CloseParen,
        Comma,
        Dot,
        Colon,
        Semicolon,
        Equals,
        Plus,
        Minus,
        Asterisk,
        Slash,
        LessThan,
        GreaterThan,
        Exclamation,
        Ampersand,
        At,
        Dollar,
        Hash,
        Percent,
        Arrow,
        AtArrow,

        // Compound punctuation
        EqualsEquals,
        PlusPlus,
        MinusMinus,
        PlusEquals,
        MinusEquals,
        AsteriskEquals,
        SlashEquals,
        LessThanEquals,
        GreaterThanEquals,
        ExclamationEquals,
        BarBar,
        AmpersandAmpersand,

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
