using System;
using System.Collections.Generic;

namespace SciAdvNet.NSScript
{
    public class SyntaxVisitor
    {
        public void Visit(SyntaxNode node)
        {
            node.Accept(this);
        }

        private void DefaultVisitNode(SyntaxNode node) { }

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

        public virtual void VisitMethod(Method method)
        {
            DefaultVisitNode(method);
        }

        public virtual void VisitBlock(Block block)
        {
            DefaultVisitNode(block);
        }

        public virtual void VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            DefaultVisitNode(expressionStatement);
        }

        public virtual void VisitLiteral(Literal literal)
        {
            DefaultVisitNode(literal);
        }

        public virtual void VisitConstantValue(ConstantValue constantValue)
        {
            DefaultVisitNode(constantValue);
        }

        public virtual void VisitIdentifier(Identifier identifier)
        {
            DefaultVisitNode(identifier);
        }

        public virtual void VisitNamedConstant(NamedConstant namedConstant)
        {
            DefaultVisitNode(namedConstant);
        }

        public virtual void VisitVariable(Variable variable)
        {
            DefaultVisitNode(variable);
        }

        public virtual void VisitParameterReference(ParameterReference parameterReference)
        {
            DefaultVisitNode(parameterReference);
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

        public virtual void VisitMethodCall(MethodCall methodCall)
        {
            DefaultVisitNode(methodCall);
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
            throw new NotImplementedException();
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

        public virtual void VisitVoice(Voice voice)
        {
            DefaultVisitNode(voice);
        }

        public virtual void VisitDialogueLine(DialogueLine dialogueLine)
        {
            DefaultVisitNode(dialogueLine);
        }

        public virtual void VisitPXmlContent(PXmlContent pXmlContent)
        {

        }

        public virtual void VisitPXmlText(PXmlText pXmlText)
        {
        }

        public virtual void VisitColorElement(ColorElement colorElement)
        {
        }
    }

    public class SyntaxVisitor<TResult>
    {
        public TResult Visit(SyntaxNode node)
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

        private TResult DefaultVisitNode(SyntaxNode node) => default(TResult);

        public virtual TResult VisitChapter(Chapter chapter)
        {
            return DefaultVisitNode(chapter);
        }

        public virtual TResult VisitMethod(Method method)
        {
            return DefaultVisitNode(method);
        }

        public virtual TResult VisitBlock(Block block)
        {
            return DefaultVisitNode(block);
        }

        public virtual TResult VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            return DefaultVisitNode(expressionStatement);
        }

        public virtual TResult VisitLiteral(Literal literal)
        {
            return DefaultVisitNode(literal);
        }

        public virtual TResult VisitConstantValue(ConstantValue constantValue)
        {
            return DefaultVisitNode(constantValue);
        }

        public virtual TResult VisitIdentifier(Identifier identifier)
        {
            return DefaultVisitNode(identifier);
        }

        public virtual TResult VisitNamedConstant(NamedConstant namedConstant)
        {
            return DefaultVisitNode(namedConstant);
        }

        public virtual TResult VisitVariable(Variable variable)
        {
            return DefaultVisitNode(variable);
        }

        public virtual TResult VisitParameterReference(ParameterReference parameterReference)
        {
            return DefaultVisitNode(parameterReference);
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

        public virtual TResult VisitMethodCall(MethodCall methodCall)
        {
            return DefaultVisitNode(methodCall);
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

        public TResult VisitCallSceneStatement(CallSceneStatement callSceneStatement)
        {
            return DefaultVisitNode(callSceneStatement);
        }

        public TResult VisitCallChapterStatement(CallChapterStatement callChapterStatement)
        {
            return DefaultVisitNode(callChapterStatement);
        }

        public virtual TResult VisitDialogueBlock(DialogueBlock dialogueBlock)
        {
            return DefaultVisitNode(dialogueBlock);
        }

        public virtual TResult VisitVoice(Voice voice)
        {
            return DefaultVisitNode(voice);
        }

        public virtual TResult VisitDialogueLine(DialogueLine dialogueLine)
        {
            return DefaultVisitNode(dialogueLine);
        }

        public virtual TResult VisitScene(Scene scene)
        {
            return DefaultVisitNode(scene);
        }
    }
}
