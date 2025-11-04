using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax;

public abstract class SyntaxVisitor
{
    public void Visit(SyntaxNode? node)
    {
        node?.Accept(this);
    }

    protected virtual void DefaultVisitNode(SyntaxNode node) { }

    protected void VisitChildren(SyntaxNode node)
    {
        foreach (SyntaxNode child in node.GetChildren())
        {
            Visit(child);
        }
    }

    public virtual void VisitSourceFileRoot(SourceFileRoot root)
    {
        DefaultVisitNode(root);
    }

    public virtual void VisitChapter(ChapterDeclaration chapter)
    {
        DefaultVisitNode(chapter);
    }

    public virtual void VisitScene(SceneDeclaration scene)
    {
        DefaultVisitNode(scene);
    }

    public virtual void VisitFunction(FunctionDeclaration function)
    {
        DefaultVisitNode(function);
    }

    public virtual void VisitParameter(Parameter parameter)
    {
        DefaultVisitNode(parameter);
    }

    public virtual void VisitBlock(Block block)
    {
        DefaultVisitNode(block);
    }

    public virtual void VisitExpressionStatement(ExpressionStatement expressionStatement)
    {
        DefaultVisitNode(expressionStatement);
    }

    public virtual void VisitIfStatement(IfStatement ifStatement)
    {
        DefaultVisitNode(ifStatement);
    }

    public virtual void VisitBreakStatement(BreakStatement breakStatement)
    {
        DefaultVisitNode(breakStatement);
    }

    public virtual void VisitWhileStatement(WhileStatement whileStatement)
    {
        DefaultVisitNode(whileStatement);
    }

    public virtual void VisitReturnStatement(ReturnStatement returnStatement)
    {
        DefaultVisitNode(returnStatement);
    }

    public virtual void VisitSelectStatement(SelectStatement selectStatement)
    {
        DefaultVisitNode(selectStatement);
    }

    public virtual void VisitSelectSection(SelectSection selectSection)
    {
        DefaultVisitNode(selectSection);
    }

    public virtual void VisitCallChapterStatement(CallChapterStatement callChapterStatement)
    {
        DefaultVisitNode(callChapterStatement);
    }

    public virtual void VisitCallSceneStatement(CallSceneStatement callSceneStatement)
    {
        DefaultVisitNode(callSceneStatement);
    }

    public virtual void VisitDialogueBlock(DialogueBlock dialogueBlock)
    {
        DefaultVisitNode(dialogueBlock);
    }

    public virtual void VisitDialogueCodeBlock(DialogueBlockPart.CodeBlock codeBlock)
    {
        DefaultVisitNode(codeBlock);
    }

    public virtual void VisitDialogueMarkup(DialogueBlockPart.Markup markup)
    {
        DefaultVisitNode(markup);
    }

    public virtual void VisitLiteral(LiteralExpression literal)
    {
        DefaultVisitNode(literal);
    }

    public virtual void VisitNameExpression(NameExpression name)
    {
        DefaultVisitNode(name);
    }

    public virtual void VisitUnaryExpression(UnaryExpression unaryExpression)
    {
        DefaultVisitNode(unaryExpression);
    }

    public virtual void VisitBinaryExpression(BinaryExpression binaryExpression)
    {
        DefaultVisitNode(binaryExpression);
    }

    public virtual void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
    {
        DefaultVisitNode(assignmentExpression);
    }

    public virtual void VisitFunctionCall(FunctionCallExpression functionCall)
    {
        DefaultVisitNode(functionCall);
    }

    public virtual void VisitBezierExpression(BezierExpression bezierExpression)
    {
        DefaultVisitNode(bezierExpression);
    }

    public virtual void VisitErrorExpression(ErrorExpression error)
    {
        DefaultVisitNode(error);
    }

    public virtual void VisitErrorStatement(ErrorStatement error)
    {
        DefaultVisitNode(error);
    }

    protected void VisitArray(ImmutableArray<Statement> statements)
    {
        foreach (Statement statement in statements)
        {
            Visit(statement);
        }
    }

    protected void VisitArray(ImmutableArray<Expression> expressions)
    {
        foreach (Expression expression in expressions)
        {
            Visit(expression);
        }
    }

    protected void VisitArray(ImmutableArray<DialogueBlock> dialogueBlocks)
    {
        foreach (DialogueBlock dialogueBlock in dialogueBlocks)
        {
            Visit(dialogueBlock);
        }
    }
}
