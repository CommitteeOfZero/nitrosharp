namespace NitroSharp.NsScript
{
    public enum SyntaxNodeKind
    {
        ConstantValue,
        Identifier,
        Literal,
        DeltaExpression,
        UnaryExpression,
        BinaryExpression,
        AssignmentExpression,

        Chapter,
        Block,
        IfStatement,
        WhileStatement,
        FunctionCall,
        ExpressionStatement,
        Function, 
        Variable,
        NamedConstant,
        Parameter,
        DialogueLine,
        ReturnStatement,
        SelectStatement,
        SelectSection,
        CallSceneStatement,
        CallChapterStatement,
        Scene,
        BreakStatement,
        
        Paragraph,
        PXmlString,
        PXmlLineSeparator
    }
}
