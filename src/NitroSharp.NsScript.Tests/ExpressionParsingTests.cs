using System.Collections.Generic;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Syntax;
using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class ExpressionParsingTests
    {
        [Theory]
        [MemberData(nameof(GetLiteralParsingTestData))]
        public void Literals_Parse_Correctly(string text, ConstantValue expectedValue)
        {
            var expr = AssertExpression<LiteralExpression>(text, SyntaxNodeKind.LiteralExpression);
            Assert.Equal(expectedValue, expr.Value);
        }

        public static IEnumerable<object[]> GetLiteralParsingTestData()
        {
            yield return new object[] { "\"foo\"", ConstantValue.String("foo") };
            yield return new object[] { "42", ConstantValue.Number(42) };
            yield return new object[] { "true", ConstantValue.True };
            yield return new object[] { "false", ConstantValue.False };
            yield return new object[] { "null", ConstantValue.Null };
            yield return new object[] { "\"@CH25\"", ConstantValue.String("@CH25") };
            yield return new object[] { "#FFFFFF", ConstantValue.Number(0xFFFFFF) };
            yield return new object[] { "#000000", ConstantValue.Number(0) };
        }

        [Fact]
        public void At_Symbol_Plus_StringLiteral()
        {
            string text = "\"@\" + \"CH25\"";
            var expr = AssertExpression<BinaryExpression>(text, SyntaxNodeKind.BinaryExpression);
            Assert.Equal(BinaryOperatorKind.Add, expr.OperatorKind.Value);
            Assert.IsType<LiteralExpression>(expr.Left);
            Assert.IsType<LiteralExpression>(expr.Right);
        }

        [Fact]
        public void At_Symbol_Plus_Identifier()
        {
            string text = "\"@\"+$goo";
            var expr = AssertExpression<BinaryExpression>(text, SyntaxNodeKind.BinaryExpression);
            Assert.Equal(BinaryOperatorKind.Add, expr.OperatorKind.Value);
            Assert.IsType<LiteralExpression>(expr.Left);
            var rhs = Assert.IsType<NameExpression>(expr.Right);
            Assert.Equal("goo", rhs.Name);
        }

        [Fact]
        public void DeltaOperator()
        {
            var expr = AssertExpression<UnaryExpression>("@42", SyntaxNodeKind.UnaryExpression);
            Assert.Equal(UnaryOperatorKind.Delta, expr.OperatorKind.Value);
            var operand = Assert.IsType<LiteralExpression>(expr.Operand);
            Assert.Equal(ConstantValue.Number(42), operand.Value);
        }

        public static IEnumerable<object[]> GetDialogueBlockTestData()
        {
            yield return new object[] { };
        }

        [Theory]
        [InlineData("Foo()", "Foo")]
        public void FunctionCall(string text, string functionName)
        {
            var invocation = AssertExpression<FunctionCallExpression>(text, SyntaxNodeKind.FunctionCallExpression);
            Common.AssertSpannedText(text, functionName, invocation.TargetName);
        }

        private T AssertExpression<T>(string text, SyntaxNodeKind expectedKind) where T : Expression
        {
            var result = Assert.IsType<T>(Parsing.ParseExpression(text).Root);
            Assert.Equal(expectedKind, result.Kind);
            return result;
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
