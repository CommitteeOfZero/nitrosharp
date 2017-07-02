using System.Collections.Generic;

namespace NitroSharp.NsScript.Execution
{
    public class ExpressionFlattener : SyntaxVisitor
    {
        private Stack<Expression> _operandStack;
        private Stack<OperationKind> _operationStack;

        public void Flatten(Expression expression, Stack<Expression> operandStack, Stack<OperationKind> operationStack)
        {
            _operandStack = operandStack;
            _operationStack = operationStack;
            Visit(expression);
        }

        public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            _operationStack.Push(assignmentExpression.OperationKind);

            Visit(assignmentExpression.Target);
            Visit(assignmentExpression.Value);
        }

        public override void VisitBinaryExpression(BinaryExpression binaryExpression)
        {
            _operationStack.Push(binaryExpression.OperationKind);

            Visit(binaryExpression.Left);
            Visit(binaryExpression.Right);
        }

        public override void VisitUnaryExpression(UnaryExpression unaryExpression)
        {
            if (OperationInfo.IsPrefixUnary(unaryExpression.OperationKind))
            {
                _operationStack.Push(unaryExpression.OperationKind);
            }

            Visit(unaryExpression.Operand);

            if (OperationInfo.IsPostfixUnary(unaryExpression.OperationKind))
            {
                _operationStack.Push(unaryExpression.OperationKind);
            }
        }

        public override void VisitVariable(Variable variable)
        {
            _operandStack.Push(variable);
            _operationStack.Push(OperationKind.NoOp);
        }

        public override void VisitParameterReference(ParameterReference parameterReference)
        {
            _operandStack.Push(parameterReference);
            _operationStack.Push(OperationKind.NoOp);
        }

        public override void VisitDeltaExpression(DeltaExpression deltaExpression)
        {
            _operandStack.Push(deltaExpression);
            _operationStack.Push(OperationKind.NoOp);
        }

        public override void VisitLiteral(Literal literal)
        {
            _operandStack.Push(literal.Value);
            _operationStack.Push(OperationKind.NoOp);
        }

        public override void VisitConstantValue(ConstantValue constantValue)
        {
            _operandStack.Push(constantValue);
            _operationStack.Push(OperationKind.NoOp);
        }

        public override void VisitFunctionCall(FunctionCall functionCall)
        {
            _operandStack.Push(functionCall);
            _operationStack.Push(OperationKind.NoOp);
        }

        public override void VisitNamedConstant(NamedConstant namedConstant)
        {
            _operandStack.Push(namedConstant);
            _operationStack.Push(OperationKind.NoOp);
        }
    }
}
