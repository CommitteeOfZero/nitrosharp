using System.Collections.Generic;

namespace NitroSharp.NsScript.Syntax
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

        public virtual TResult VisitChapter(ChapterDeclaration chapter)
        {
            return DefaultVisitNode(chapter);
        }

        public virtual TResult VisitScene(SceneDeclaration scene)
        {
            return DefaultVisitNode(scene);
        }

        public virtual TResult VisitFunction(FunctionDeclaration function)
        {
            return DefaultVisitNode(function);
        }

        public virtual TResult VisitParameter(Parameter parameter)
        {
            return DefaultVisitNode(parameter);
        }

        public virtual TResult VisitBlock(Block block)
        {
            return DefaultVisitNode(block);
        }

        public virtual TResult VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            return DefaultVisitNode(expressionStatement);
        }

        public virtual TResult VisitLiteral(LiteralExpression literal)
        {
            return DefaultVisitNode(literal);
        }

        public virtual TResult VisitUnaryExpression(UnaryExpression unaryExpression)
        {
            return DefaultVisitNode(unaryExpression);
        }

        public virtual TResult VisitBinaryExpression(BinaryExpression binaryExpression)
        {
            return DefaultVisitNode(binaryExpression);
        }

        public virtual TResult VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            return DefaultVisitNode(assignmentExpression);
        }

        public virtual TResult VisitFunctionCall(FunctionCallExpression functionCall)
        {
            return DefaultVisitNode(functionCall);
        }

        public virtual TResult VisitIfStatement(IfStatement ifStatement)
        {
            return DefaultVisitNode(ifStatement);
        }

        public virtual TResult VisitBreakStatement(BreakStatement breakStatement)
        {
            return DefaultVisitNode(breakStatement);
        }

        public virtual TResult VisitWhileStatement(WhileStatement whileStatement)
        {
            return DefaultVisitNode(whileStatement);
        }

        public virtual TResult VisitReturnStatement(ReturnStatement returnStatement)
        {
            return DefaultVisitNode(returnStatement);
        }

        public virtual TResult VisitSelectStatement(SelectStatement selectStatement)
        {
            return DefaultVisitNode(selectStatement);
        }

        public virtual TResult VisitSelectSection(SelectSection selectSection)
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

        public virtual TResult VisitMarkupNode(MarkupNode node)
        {
            return DefaultVisitNode(node);
        }

        public virtual TResult VisitDialogueBlock(DialogueBlock dialogueBlock)
        {
            return DefaultVisitNode(dialogueBlock);
        }

        public virtual TResult VisitMarkupBlankLine(MarkupBlankLine blankLine)
        {
            return DefaultVisitNode(blankLine);
        }
    }
}
