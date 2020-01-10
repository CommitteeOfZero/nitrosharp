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
        FunctionCallExpression,
        BezierExpression,


        DialogueBlock,
        PXmlString,
        PXmlLineSeparator
    }
}
