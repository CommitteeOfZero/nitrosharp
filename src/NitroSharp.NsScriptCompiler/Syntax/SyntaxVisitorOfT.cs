using System.Collections.Generic;

namespace NitroSharp.NsScriptNew.Syntax
{
    public abstract class SyntaxVisitor<TResult>
    {
        public virtual TResult Visit(SyntaxNode node)
        {
            return node.Accept(this);
        }

        protected void VisitArray(IEnumerable<SyntaxNode> list)
        {
            foreach (var node in list)
            {
                Visit(node);
            }
        }

        private TResult DefaultVisitNode(SyntaxNode node) => default(TResult)!;

        //public virtual TResult VisitSourceFile(SourceFile sourceFile)
        //{
        //    return DefaultVisitNode(sourceFile);
        //}

        public virtual TResult VisitChapter(ChapterDeclarationSyntax chapter)
        {
            return DefaultVisitNode(chapter);
        }

        public virtual TResult VisitScene(SceneDeclarationSyntax scene)
        {
            return DefaultVisitNode(scene);
        }

        public virtual TResult VisitFunction(FunctionDeclarationSyntax function)
        {
            return DefaultVisitNode(function);
        }

        public virtual TResult VisitParameter(ParameterSyntax parameter)
        {
            return DefaultVisitNode(parameter);
        }

        public virtual TResult VisitBlock(BlockSyntax block)
        {
            return DefaultVisitNode(block);
        }

        public virtual TResult VisitExpressionStatement(ExpressionStatementSyntax expressionStatement)
        {
            return DefaultVisitNode(expressionStatement);
        }

        public virtual TResult VisitLiteral(LiteralExpressionSyntax literal)
        {
            return DefaultVisitNode(literal);
        }

        public virtual TResult VisitUnaryExpression(UnaryExpressionSyntax unaryExpression)
        {
            return DefaultVisitNode(unaryExpression);
        }

        public virtual TResult VisitBinaryExpression(BinaryExpressionSyntax binaryExpression)
        {
            return DefaultVisitNode(binaryExpression);
        }

        public virtual TResult VisitAssignmentExpression(AssignmentExpressionSyntax assignmentExpression)
        {
            return DefaultVisitNode(assignmentExpression);
        }

        public virtual TResult VisitDeltaExpression(DeltaExpressionSyntax deltaExpression)
        {
            return DefaultVisitNode(deltaExpression);
        }

        public virtual TResult VisitFunctionCall(FunctionCallExpressionSyntax functionCall)
        {
            return DefaultVisitNode(functionCall);
        }

        public virtual TResult VisitIfStatement(IfStatementSyntax ifStatement)
        {
            return DefaultVisitNode(ifStatement);
        }

        public virtual TResult VisitBreakStatement(BreakStatementSyntax breakStatement)
        {
            return DefaultVisitNode(breakStatement);
        }

        public virtual TResult VisitWhileStatement(WhileStatementSyntax whileStatement)
        {
            return DefaultVisitNode(whileStatement);
        }

        public virtual TResult VisitReturnStatement(ReturnStatementSyntax returnStatement)
        {
            return DefaultVisitNode(returnStatement);
        }

        public virtual TResult VisitSelectStatement(SelectStatementSyntax selectStatement)
        {
            return DefaultVisitNode(selectStatement);
        }

        public virtual TResult VisitSelectSection(SelectSectionSyntax selectSection)
        {
            return DefaultVisitNode(selectSection);
        }

        //public TResult VisitCallSceneStatement(CallSceneStatement callSceneStatement)
        //{
        //    return DefaultVisitNode(callSceneStatement);
        //}

        //public TResult VisitCallChapterStatement(CallChapterStatement callChapterStatement)
        //{
        //    return DefaultVisitNode(callChapterStatement);
        //}

        public virtual TResult VisitPXmlString(PXmlString pxmlString)
        {
            return DefaultVisitNode(pxmlString);
        }

        public virtual TResult VisitDialogueBlock(DialogueBlockSyntax dialogueBlock)
        {
            return DefaultVisitNode(dialogueBlock);
        }

        public virtual TResult VisitPXmlLineSeparator(PXmlLineSeparator pxmlLineSeparator)
        {
            return DefaultVisitNode(pxmlLineSeparator);
        }
    }
}
