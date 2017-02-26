using System.IO;

namespace SciAdvNet.NSScript
{
    public abstract class SyntaxNode
    {
        public abstract SyntaxNodeKind Kind { get; }
        public abstract void Accept(SyntaxVisitor visitor);
        public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);

        public override string ToString()
        {
            var sw = new StringWriter();
            var codeWriter = new DefaultCodeWriter(sw);
            codeWriter.WriteNode(this);

            return sw.ToString();
        }
    }

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
        MethodCall,
        ExpressionStatement,
        Method,
        DialogueBlock,
        Script,
        Variable,
        NamedConstant,
        Parameter,
        SimpleName,
        DialogueLine,
        TextSegment,
        Voice,
        ScriptRoot,
        ReturnStatement,
        SelectStatement,
        SelectSection,
        CallSceneStatement,
        CallChapterStatement,
        Scene,
        BreakStatement,
        PXmlContent,
        ColorElement,
        RubyElement,
        PXmlText,
        PXmlVerbatimText
    }
}
