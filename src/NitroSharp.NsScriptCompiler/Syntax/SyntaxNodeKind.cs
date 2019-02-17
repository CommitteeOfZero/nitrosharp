namespace NitroSharp.NsScript.Syntax
{
    public enum SyntaxNodeKind
    {
        SourceFileRoot,

        ChapterDeclaration,
        SceneDeclaration,
        FunctionDeclaration,
        Parameter,

        Block,
        IfStatement,
        WhileStatement,
        FunctionCallExpression,
        ExpressionStatement,
        ReturnStatement,
        SelectStatement,
        SelectSection,
        CallSceneStatement,
        CallChapterStatement,
        BreakStatement,

        NameExpression,
        LiteralExpression,
        UnaryExpression,
        BinaryExpression,
        AssignmentExpression,
        
        DialogueBlock,
        PXmlString,
        PXmlLineSeparator
    }
}
