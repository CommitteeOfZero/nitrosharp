namespace NitroSharp.NsScriptNew.Syntax
{
    public enum SyntaxNodeKind
    {
        SourceFile,

        Chapter,
        Scene,
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

        Identifier,
        Literal,
        UnaryExpression,
        BinaryExpression,
        AssignmentExpression,
        DeltaExpression,
        
        DialogueBlock,
        PXmlString,
        PXmlLineSeparator
    }
}
