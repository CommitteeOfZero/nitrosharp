using NitroSharp.NsScriptNew;
using NitroSharp.NsScriptNew.Syntax;
using Xunit;

namespace NitroSharp.NsScriptCompiler.Tests
{
    public class ExpressionParsingTests
    {
        private T ParseExpression<T>(string text) where T : Expression
        {
            return Assert.IsType<T>(Parsing.ParseExpression(text).Root);
        }

        [Fact]
        public void ParseFunctionCall()
        {
            string text = "WaitKey(10000)";
            var call = ParseExpression<FunctionCall>(text);
            Assert.Equal(SyntaxNodeKind.FunctionCall, call.Kind);
            Assert.Equal("WaitKey", call.Target);
            Assert.Single(call.Arguments);

            //Assert.Equal(text, call.ToString());
        }

        [Fact]
        public void ParseDeltaExpression()
        {
            string text = "@100";
            var deltaExpr = Parsing.ParseExpression(text).Root as DeltaExpression;

            Assert.NotNull(deltaExpr);
            Assert.Equal(SyntaxNodeKind.DeltaExpression, deltaExpr.Kind);
            Assert.NotNull(deltaExpr.Expression);
        }

        [Fact]
        public void ParseUnaryOperators()
        {
            TestUnary(UnaryOperatorKind.Not);
            TestUnary(UnaryOperatorKind.Minus);
            TestUnary(UnaryOperatorKind.Plus);
        }

        [Fact]
        public void ParseBinaryOperators()
        {
            TestBinary(BinaryOperatorKind.Add);
            TestBinary(BinaryOperatorKind.Divide);
            TestBinary(BinaryOperatorKind.Equals);
            TestBinary(BinaryOperatorKind.GreaterThan);
            TestBinary(BinaryOperatorKind.GreaterThanOrEqual);
            TestBinary(BinaryOperatorKind.LessThan);
            TestBinary(BinaryOperatorKind.LessThanOrEqual);
            TestBinary(BinaryOperatorKind.And);
            TestBinary(BinaryOperatorKind.Or);
            TestBinary(BinaryOperatorKind.Multiply);
            TestBinary(BinaryOperatorKind.NotEquals);
            TestBinary(BinaryOperatorKind.Subtract);
            TestBinary(BinaryOperatorKind.Remainder);
        }

        [Fact]
        public void ParseAssignmentOperators()
        {
            TestAssignment(AssignmentOperatorKind.AddAssign);
            TestAssignment(AssignmentOperatorKind.DivideAssign);
            TestAssignment(AssignmentOperatorKind.MultiplyAssign);
            TestAssignment(AssignmentOperatorKind.Assign);
            TestAssignment(AssignmentOperatorKind.SubtractAssign);
        }

        [Fact]
        public void ParseIncrement()
        {
            string text = "$a++";
            var expr = Parsing.ParseExpression(text).Root as AssignmentExpression;
            Assert.NotNull(expr);
            Assert.Equal(AssignmentOperatorKind.Increment, expr.OperatorKind);
            Assert.Equal(expr.Target, expr.Value);
        }
        
        [Fact]
        public void ParseDecrement()
        {
            string text = "$a--";
            var expr = Parsing.ParseExpression(text).Root as AssignmentExpression;
            Assert.NotNull(expr);
            Assert.Equal(AssignmentOperatorKind.Decrement, expr.OperatorKind);
            Assert.Equal(expr.Target, expr.Value);
        }

        private void TestUnary(UnaryOperatorKind kind)
        {
            string text = OperatorInfo.GetText(kind) + "$a";
            var expr = Parsing.ParseExpression(text).Root as UnaryExpression;

            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.UnaryExpression, expr.Kind);
            Assert.Equal(kind, expr.OperatorKind);

            var operand = expr.Operand as Identifier;
            Assert.NotNull(operand);
            Assert.Equal("a", operand.Name);
        }

        private void TestBinary(BinaryOperatorKind kind)
        {
            string text = "$a " + OperatorInfo.GetText(kind) + " $b";
            var expr = Parsing.ParseExpression(text).Root as BinaryExpression;

            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.BinaryExpression, expr.Kind);
            Assert.Equal(kind, expr.OperatorKind);

            var left = expr.Left as Identifier;
            Assert.NotNull(left);
            Assert.Equal("a", left.Name);

            var right = expr.Right as Identifier;
            Assert.NotNull(right);
            Assert.Equal("b", right.Name);
        }

        private void TestAssignment(AssignmentOperatorKind kind)
        {
            string text = "$a " + OperatorInfo.GetText(kind) + " 42";
            var expr = Parsing.ParseExpression(text).Root as AssignmentExpression;

            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.AssignmentExpression, expr.Kind);
            Assert.Equal(kind, expr.OperatorKind);

            var target = expr.Target as Identifier;
            Assert.NotNull(target);
            Assert.Equal("a", target.Name);

            var value = expr.Value as Literal;
            Assert.NotNull(value);
            //Assert.Equal(42.0d, value.Value.DoubleValue);
        }
    }
}
