namespace SciAdvNet.NSScript
{
    public enum SyntaxNodeKind
    {
        ConstantValue,
        Identifier,
        Literal,
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
        DialogueBlock,
        Script,
        Variable,
        NamedConstant,
        Parameter,
        DialogueLine,
        ScriptRoot,
        ReturnStatement,
        SelectStatement,
        SelectSection,
        CallSceneStatement,
        CallChapterStatement,
        Scene,
        BreakStatement,
        PXmlString,
        DeltaExpression
    }
}
