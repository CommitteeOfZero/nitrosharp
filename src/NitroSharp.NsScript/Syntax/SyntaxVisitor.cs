using System.Collections.Generic;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax
{
    public abstract class SyntaxVisitor
    {
        protected void Visit(SyntaxNode node)
        {
            node?.Accept(this);
        }

        private void DefaultVisitNode(SyntaxNode node) { }

        protected void VisitArray(ImmutableArray<Statement> list)
        {
            foreach (var node in list)
            {
                Visit(node);
            }
        }

        protected void VisitArray(ImmutableArray<Expression> list)
        {
            foreach (var node in list)
            {
                Visit(node);
            }
        }

        protected void VisitArray(ImmutableArray<SubroutineDeclaration> list)
        {
            foreach (var node in list)
            {
                Visit(node);
            }
        }

        public virtual void VisitChapter(ChapterDeclaration chapter)
        {
            DefaultVisitNode(chapter);
        }

        public virtual void VisitFunction(FunctionDeclaration function)
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

        //public virtual void VisitSourceFile(SourceFile sourceFile)
        //{
        //    DefaultVisitNode(sourceFile);
        //}

        public virtual void VisitLiteral(LiteralExpression literal)
        {
            DefaultVisitNode(literal);
        }

        //public virtual void VisitIdentifier(Identifier identifier)
        //{
        //    DefaultVisitNode(identifier);
        //}

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

        public virtual void VisitFunctionCall(FunctionCallExpression functionCall)
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

        public virtual void VisitScene(SceneDeclaration scene)
        {
            DefaultVisitNode(scene);
        }

        public virtual void VisitSelectSection(SelectSection selectSection)
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

        public virtual void VisitMarkupNode(MarkupNode node)
        {
            DefaultVisitNode(node);
        }

        public virtual void VisitDialogueBlock(DialogueBlock dialogueBlock)
        {
            DefaultVisitNode(dialogueBlock);
        }

        public virtual void VisitMarkupBlankLine(MarkupBlankLine blankLine)
        {
            DefaultVisitNode(blankLine);
        }
    }
}
