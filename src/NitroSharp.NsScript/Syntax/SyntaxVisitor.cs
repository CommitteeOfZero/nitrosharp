using System.Collections.Generic;

namespace NitroSharp.NsScript.Syntax
{
    public abstract class SyntaxVisitor
    {
        public virtual void Visit(SyntaxNode node)
        {
            node?.Accept(this);
        }

        protected virtual void DefaultVisitNode(SyntaxNode node) { }

        public void VisitArray(IEnumerable<SyntaxNode> list)
        {
            foreach (var node in list)
            {
                Visit(node);
            }
        }

        public virtual void VisitChapter(Chapter chapter)
        {
            DefaultVisitNode(chapter);
        }

        public virtual void VisitFunction(Function function)
        {
            DefaultVisitNode(function);
        }

        public virtual void VisitBlock(Block block)
        {
            DefaultVisitNode(block);
        }

        public virtual void VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            DefaultVisitNode(expressionStatement);
        }

        public virtual void VisitSourceFile(SourceFile sourceFile)
        {
            DefaultVisitNode(sourceFile);
        }

        public virtual void VisitLiteral(Literal literal)
        {
            DefaultVisitNode(literal);
        }

        public virtual void VisitIdentifier(Identifier identifier)
        {
            DefaultVisitNode(identifier);
        }

        public virtual void VisitParameter(Parameter parameter)
        {
            DefaultVisitNode(parameter);
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

        public virtual void VisitDeltaExpression(DeltaExpression deltaExpression)
        {
            DefaultVisitNode(deltaExpression);
        }

        public virtual void VisitFunctionCall(FunctionCall functionCall)
        {
            DefaultVisitNode(functionCall);
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

        public void VisitScene(Scene scene)
        {
            DefaultVisitNode(scene);
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

        public virtual void VisitPXmlString(PXmlString pxmlString)
        {
            DefaultVisitNode(pxmlString);
        }

        public virtual void VisitParagraph(Paragraph paragraph)
        {
            DefaultVisitNode(paragraph);
        }

        public virtual void VisitPXmlLineSeparator(PXmlLineSeparator pxmlLineSeparator)
        {
            DefaultVisitNode(pxmlLineSeparator);
        }
    }
}
