using NitroSharp.NsScriptNew;
using NitroSharp.NsScriptNew.Syntax;
using Xunit;

namespace NitroSharp.NsScriptCompiler.Tests
{
    public class ExpressionParsingTests
    {
        private T AssertExpression<T>(string text, SyntaxNodeKind expectedKind) where T : ExpressionSyntax
        {
            var result = Assert.IsType<T>(Parsing.ParseExpression(text).Root);
            Assert.Equal(expectedKind, result.Kind);
            return result;
        }

        //[Theory]
        //[InlineData("\"foo\"", SyntaxNodeKind.Literal, ConstantValue.String("foo"))]
        //[InlineData("42", SyntaxNodeKind.Literal, ConstantValue.Integer(42))]
        //[InlineData("true", SyntaxNodeKind.Literal, ConstantValue.True)]
        //[InlineData("false", SyntaxNodeKind.Literal)]
        //[InlineData("null", SyntaxNodeKind.Literal)]
        //public void Literals_Parse_Correctly(string text, SyntaxNodeKind expectedKind, ConstantValue expectedValue)
        //{

        //}

        [Theory]
        [InlineData("Foo()", "Foo")]
        public void FunctionCall_Parses_Correctly(string text, string functionName)
        {
            var invocation = AssertExpression<FunctionCallSyntax>(text, SyntaxNodeKind.FunctionCall);
            Common.AssertSpannedText(text, functionName, invocation.TargetName);
        }

        //[Fact]
        //public void ParseFunctionCall()
        //{
        //    string text = "WaitKey(10000)";
        //    var call = ParseExpression<FunctionCallSyntax>(text);
        //    Assert.Equal(SyntaxNodeKind.FunctionCall, call.Kind);
        //    Assert.Equal("WaitKey", call.TargetName);
        //    Assert.Single(call.Arguments);

        //    //Assert.Equal(text, call.ToString());
        //}

        //[Fact]
        //public void ParseDeltaExpression()
        //{
        //    string text = "@100";
        //    var deltaExpr = Parsing.ParseExpression(text).Root as DeltaExpressionSyntax;

        //    Assert.NotNull(deltaExpr);
        //    Assert.Equal(SyntaxNodeKind.DeltaExpression, deltaExpr.Kind);
        //    Assert.NotNull(deltaExpr.Expression);
        //}

        //[Fact]
        //public void ParseUnaryOperators()
        //{
        //    TestUnary(UnaryOperatorKind.Not);
        //    TestUnary(UnaryOperatorKind.Minus);
        //    TestUnary(UnaryOperatorKind.Plus);
        //}

        //[Fact]
        //public void ParseBinaryOperators()
        //{
        //    TestBinary(BinaryOperatorKind.Add);
        //    TestBinary(BinaryOperatorKind.Divide);
        //    TestBinary(BinaryOperatorKind.Equals);
        //    TestBinary(BinaryOperatorKind.GreaterThan);
        //    TestBinary(BinaryOperatorKind.GreaterThanOrEqual);
        //    TestBinary(BinaryOperatorKind.LessThan);
        //    TestBinary(BinaryOperatorKind.LessThanOrEqual);
        //    TestBinary(BinaryOperatorKind.And);
        //    TestBinary(BinaryOperatorKind.Or);
        //    TestBinary(BinaryOperatorKind.Multiply);
        //    TestBinary(BinaryOperatorKind.NotEquals);
        //    TestBinary(BinaryOperatorKind.Subtract);
        //    TestBinary(BinaryOperatorKind.Remainder);
        //}

        //[Fact]
        //public void ParseAssignmentOperators()
        //{
        //    TestAssignment(AssignmentOperatorKind.AddAssign);
        //    TestAssignment(AssignmentOperatorKind.DivideAssign);
        //    TestAssignment(AssignmentOperatorKind.MultiplyAssign);
        //    TestAssignment(AssignmentOperatorKind.Assign);
        //    TestAssignment(AssignmentOperatorKind.SubtractAssign);
        //}

        //[Fact]
        //public void ParseIncrement()
        //{
        //    string text = "$a++";
        //    var expr = Parsing.ParseExpression(text).Root as AssignmentExpressionSyntax;
        //    Assert.NotNull(expr);
        //    Assert.Equal(AssignmentOperatorKind.Increment, expr.OperatorKind);
        //    Assert.Equal(expr.Target, expr.Value);
        //}

        //[Fact]
        //public void ParseDecrement()
        //{
        //    string text = "$a--";
        //    var expr = Parsing.ParseExpression(text).Root as AssignmentExpressionSyntax;
        //    Assert.NotNull(expr);
        //    Assert.Equal(AssignmentOperatorKind.Decrement, expr.OperatorKind);
        //    Assert.Equal(expr.Target, expr.Value);
        //}

        //private void TestUnary(UnaryOperatorKind kind)
        //{
        //    string text = OperatorInfo.GetText(kind) + "$a";
        //    var expr = Parsing.ParseExpression(text).Root as UnaryExpressionSyntax;

        //    Assert.NotNull(expr);
        //    Assert.Equal(SyntaxNodeKind.UnaryExpression, expr.Kind);
        //    Assert.Equal(kind, expr.OperatorKind);

        //    var operand = expr.Operand as NameSyntax;
        //    Assert.NotNull(operand);
        //    Assert.Equal("a", operand.Name);
        //}

        //private void TestBinary(BinaryOperatorKind kind)
        //{
        //    string text = "$a " + OperatorInfo.GetText(kind) + " $b";
        //    var expr = Parsing.ParseExpression(text).Root as BinaryExpressionSyntax;

        //    Assert.NotNull(expr);
        //    Assert.Equal(SyntaxNodeKind.BinaryExpression, expr.Kind);
        //    Assert.Equal(kind, expr.OperatorKind);

        //    var left = expr.Left as NameSyntax;
        //    Assert.NotNull(left);
        //    Assert.Equal("a", left.Name);

        //    var right = expr.Right as NameSyntax;
        //    Assert.NotNull(right);
        //    Assert.Equal("b", right.Name);
        //}

        //private void TestAssignment(AssignmentOperatorKind kind)
        //{
        //    string text = "$a " + OperatorInfo.GetText(kind) + " 42";
        //    var expr = Parsing.ParseExpression(text).Root as AssignmentExpressionSyntax;

        //    Assert.NotNull(expr);
        //    Assert.Equal(SyntaxNodeKind.AssignmentExpression, expr.Kind);
        //    Assert.Equal(kind, expr.OperatorKind);

        //    var target = expr.Target as NameSyntax;
        //    Assert.NotNull(target);
        //    Assert.Equal("a", target.Name);

        //    var value = expr.Value as LiteralExpressionSyntax;
        //    Assert.NotNull(value);
        //    //Assert.Equal(42.0d, value.Value.DoubleValue);
        //}
    }
}
