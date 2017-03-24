using System.Collections.Generic;

namespace SciAdvNet.NSScript.Execution
{
    public class ExpressionVisitor : SyntaxVisitor
    {
        public ExpressionVisitor()
        {
            OperandStack = new Stack<Expression>();
            OperationStack = new Stack<OperationKind>();
        }

        public Stack<Expression> OperandStack { get; }
        public Stack<OperationKind> OperationStack { get; }

        public void Flatten(Expression expression)
        {
            OperandStack.Clear();
            OperationStack.Clear();

            Visit(expression);
        }

        public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            OperationStack.Push(assignmentExpression.OperationKind);

            Visit(assignmentExpression.Target);
            Visit(assignmentExpression.Value);
        }

        public override void VisitBinaryExpression(BinaryExpression binaryExpression)
        {
            OperationStack.Push(binaryExpression.OperationKind);

            Visit(binaryExpression.Left);
            Visit(binaryExpression.Right);
        }

        public override void VisitVariable(Variable variable)
        {
            OperandStack.Push(variable);
            OperationStack.Push(OperationKind.NoOp);
        }

        public override void VisitLiteral(Literal literal)
        {
            OperandStack.Push(literal.Value);
            OperationStack.Push(OperationKind.NoOp);
        }
    }
}
