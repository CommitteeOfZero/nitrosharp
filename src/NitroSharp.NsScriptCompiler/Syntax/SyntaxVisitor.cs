using System.Collections.Generic;
using System.Collections.Immutable;

namespace NitroSharp.NsScriptNew.Syntax
{
    public abstract class SyntaxVisitor
    {
        protected void Visit(SyntaxNode node)
        {
            node?.Accept(this);
        }

        private void DefaultVisitNode(SyntaxNode node) { }

        protected void VisitArray(ImmutableArray<StatementSyntax> list)
        {
            foreach (var node in list)
            {
                Visit(node);
            }
        }

        protected void VisitArray(ImmutableArray<ExpressionSyntax> list)
        {
            foreach (var node in list)
            {
                Visit(node);
            }
        }

        protected void VisitArray(ImmutableArray<SubroutineDeclarationSyntax> list)
        {
            foreach (var node in list)
            {
                Visit(node);
            }
        }

        public virtual void VisitChapter(ChapterDeclarationSyntax chapter)
        {
            DefaultVisitNode(chapter);
        }

        public virtual void VisitFunction(FunctionDeclarationSyntax function)
        {
            DefaultVisitNode(function);
        }

        public virtual void VisitBlock(BlockSyntax block)
        {
            DefaultVisitNode(block);
        }

        public virtual void VisitExpressionStatement(ExpressionStatementSyntax expressionStatement)
        {
            DefaultVisitNode(expressionStatement);
        }

        //public virtual void VisitSourceFile(SourceFile sourceFile)
        //{
        //    DefaultVisitNode(sourceFile);
        //}

        public virtual void VisitLiteral(LiteralExpressionSyntax literal)
        {
            DefaultVisitNode(literal);
        }

        //public virtual void VisitIdentifier(Identifier identifier)
        //{
        //    DefaultVisitNode(identifier);
        //}

        public virtual void VisitParameter(ParameterSyntax parameter)
        {
            DefaultVisitNode(parameter);
        }

        public virtual void VisitUnaryExpression(UnaryExpressionSyntax unaryExpression)
        {
            DefaultVisitNode(unaryExpression);
        }

        public virtual void VisitBinaryExpression(BinaryExpressionSyntax binaryExpression)
        {
            DefaultVisitNode(binaryExpression);
        }

        public virtual void VisitAssignmentExpression(AssignmentExpressionSyntax assignmentExpression)
        {
            DefaultVisitNode(assignmentExpression);
        }

        public virtual void VisitDeltaExpression(DeltaExpressionSyntax deltaExpression)
        {
            DefaultVisitNode(deltaExpression);
        }

        public virtual void VisitFunctionCall(FunctionCallExpressionSyntax functionCall)
        {
            DefaultVisitNode(functionCall);
        }

        public virtual void VisitIfStatement(IfStatementSyntax ifStatement)
        {
            DefaultVisitNode(ifStatement);
        }

        public virtual void VisitBreakStatement(BreakStatementSyntax breakStatement)
        {
            DefaultVisitNode(breakStatement);
        }

        public virtual void VisitWhileStatement(WhileStatementSyntax whileStatement)
        {
            DefaultVisitNode(whileStatement);
        }

        public virtual void VisitReturnStatement(ReturnStatementSyntax returnStatement)
        {
            DefaultVisitNode(returnStatement);
        }

        public virtual void VisitSelectStatement(SelectStatementSyntax selectStatement)
        {
            DefaultVisitNode(selectStatement);
        }

        public virtual void VisitScene(SceneDeclarationSyntax scene)
        {
            DefaultVisitNode(scene);
        }

        public virtual void VisitSelectSection(SelectSectionSyntax selectSection)
        {
            DefaultVisitNode(selectSection);
        }

        //public virtual void VisitCallChapterStatement(CallChapterStatement callChapterStatement)
        //{
        //    DefaultVisitNode(callChapterStatement);
        //}

        //public virtual void VisitCallSceneStatement(CallSceneStatement callSceneStatement)
        //{
        //    DefaultVisitNode(callSceneStatement);
        //}

        public virtual void VisitPXmlString(PXmlString pxmlString)
        {
            DefaultVisitNode(pxmlString);
        }

        public virtual void VisitDialogueBlock(DialogueBlockSyntax dialogueBlock)
        {
            DefaultVisitNode(dialogueBlock);
        }

        public virtual void VisitPXmlLineSeparator(PXmlLineSeparator pxmlLineSeparator)
        {
            DefaultVisitNode(pxmlLineSeparator);
        }
    }
}
