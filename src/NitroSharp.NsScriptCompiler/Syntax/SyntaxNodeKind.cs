namespace NitroSharp.NsScriptNew.Syntax
{
    public enum SyntaxNodeKind
    {
        SourceFile,

        ChapterDeclaration,
        SceneDeclaration,
        Function,
        Parameter,

        Block,
        IfStatement,
        WhileStatement,
        FunctionCall,
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
        DeltaExpression,
        
        DialogueBlock,
        PXmlString,
        PXmlLineSeparator
    }
}
