using System.IO;

namespace NitroSharp.NsScript.Syntax
{
    public class DefaultCodeWriter : CodeWriter
    {
        public DefaultCodeWriter(TextWriter textWriter)
            : base(textWriter)
        {
        }

        public override void VisitChapter(Chapter chapter)
        {
            Write(SyntaxFacts.GetText(SyntaxTokenKind.ChapterKeyword));
            WriteSpace();
            Visit(chapter.Name);
            Visit(chapter.Body);
        }

        public override void VisitFunction(Function function)
        {
            Write(SyntaxFacts.GetText(SyntaxTokenKind.FunctionKeyword));
            WriteSpace();
            Visit(function.Name);
            Write("(");

            var parameters = function.Parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                Visit(function.Parameters[i]);
                if (i != parameters.Length - 1)
                {
                    Write(", ");
                }
            }

            Write(")");
            Visit(function.Body);
        }

        public override void VisitBlock(Block block)
        {
            WriteLine();
            Write("{");
            WriteLine();
            Indent();

            foreach (var statement in block.Statements)
            {
                Visit(statement);
                WriteLine();
            }

            Outdent();
            Write("}");
            WriteLine();
        }

        public override void VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            Visit(expressionStatement.Expression);
            Write(";");
        }

        public override void VisitDeltaExpression(DeltaExpression deltaExpression)
        {
            Write("@");
            Visit(deltaExpression.Expression);
        }

        public override void VisitLiteral(Literal literal)
        {
            Write(literal.Text);
        }

        public override void VisitIdentifier(Identifier identifier)
        {
            Write(identifier.OriginalName);
        }

        public override void VisitParameter(Parameter parameter)
        {
            Visit(parameter.Name);
        }

        public override void VisitUnaryExpression(UnaryExpression unaryExpression)
        {
            var operatorKind = unaryExpression.OperatorKind;
            if (OperatorInfo.IsPrefixUnary(operatorKind))
            {
                Write(OperatorInfo.GetText(operatorKind));
            }

            Visit(unaryExpression.Operand);

            if (OperatorInfo.IsPostfixUnary(operatorKind))
            {
                Write(OperatorInfo.GetText(operatorKind));
            }
        }

        public override void VisitBinaryExpression(BinaryExpression binaryExpression)
        {
            Visit(binaryExpression.Left);
            WriteSpace();
            Write(OperatorInfo.GetText(binaryExpression.OperatorKind));
            WriteSpace();
            Visit(binaryExpression.Right);
        }

        public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            Visit(assignmentExpression.Target);
            WriteSpace();
            Write(OperatorInfo.GetText(assignmentExpression.OperatorKind));
            WriteSpace();
            Visit(assignmentExpression.Value);
        }

        public override void VisitFunctionCall(FunctionCall functionCall)
        {
            Visit(functionCall.Target);
            Write("(");

            var args = functionCall.Arguments;
            for (int i = 0; i < args.Length; i++)
            {
                Visit(functionCall.Arguments[i]);
                if (i != args.Length - 1)
                {
                    Write(", ");
                }
            }

            Write(")");
        }

        public override void VisitIfStatement(IfStatement ifStatement)
        {
            Write(SyntaxFacts.GetText(SyntaxTokenKind.IfKeyword));
            WriteSpace();
            Write("(");
            Visit(ifStatement.Condition);
            Write(")");

            bool block = ifStatement.IfTrueStatement.Kind == SyntaxNodeKind.Block;
            if (!block)
            {
                WriteLine();
                Indent();
                Visit(ifStatement.IfTrueStatement);
                Outdent();
                WriteLine();
            }
            else
            {
                Visit(ifStatement.IfTrueStatement);
            }


            if (ifStatement.IfFalseStatement != null)
            {
                Write(SyntaxFacts.GetText(SyntaxTokenKind.ElseKeyword));

                block = ifStatement.IfFalseStatement.Kind == SyntaxNodeKind.Block;
                bool elif = ifStatement.IfFalseStatement.Kind == SyntaxNodeKind.IfStatement;
                if (!block && !elif)
                {
                    WriteLine();
                    Indent();
                    Visit(ifStatement.IfTrueStatement);
                    Outdent();
                    WriteLine();
                }
                else if (elif)
                {
                    WriteSpace();
                }

                Visit(ifStatement.IfFalseStatement);
            }
        }

        public override void VisitBreakStatement(BreakStatement breakStatement)
        {
            Write(SyntaxFacts.GetText(SyntaxTokenKind.BreakKeyword));
            Write(";");
        }

        public override void VisitWhileStatement(WhileStatement whileStatement)
        {
            Write(SyntaxFacts.GetText(SyntaxTokenKind.WhileKeyword));
            WriteSpace();
            Write("(");
            Visit(whileStatement.Condition);
            Write(")");

            if (whileStatement.Body.Kind != SyntaxNodeKind.Block)
            {
                WriteLine();
                Indent();
                Visit(whileStatement.Body);
                Outdent();
                WriteLine();
            }
            else
            {
                Visit(whileStatement.Body);
            }
        }

        public override void VisitParagraph(Paragraph dialogueBlock)
        {
            Write($"<PRE {dialogueBlock.AssociatedBox}>");
            WriteLine();
            Write($"[{dialogueBlock.Identifier}]");
            WriteLine();
            Write("</PRE>");
        }
    }
}
