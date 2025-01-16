using System.Collections.Generic;
using NitroSharp.NsScript.Syntax;
using Xunit;

namespace NitroSharp.NsScript.Tests;

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
        yield return ["\"foo\"", ConstantValue.String("foo")];
        yield return ["42", ConstantValue.Number(42)];
        yield return ["true", ConstantValue.True];
        yield return ["false", ConstantValue.False];
        yield return ["null", ConstantValue.Null];
        yield return ["\"@CH25\"", ConstantValue.String("@CH25")];
        yield return ["#FFFFFF", ConstantValue.Number(0xFFFFFF)];
        yield return ["#000000", ConstantValue.Number(0)];
    }

    [Fact]
    public void At_Symbol_Plus_StringLiteral()
    {
        const string text = "\"@\" + \"CH25\"";
        var expr = AssertExpression<BinaryExpression>(text, SyntaxNodeKind.BinaryExpression);
        Assert.Equal(BinaryOperatorKind.Add, expr.OperatorKind.Value);
        Assert.IsType<LiteralExpression>(expr.Left);
        Assert.IsType<LiteralExpression>(expr.Right);
    }

    [Fact]
    public void At_Symbol_Plus_Identifier()
    {
        const string text = "\"@\"+$goo";
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

    [Fact]
    public void ParseFunctionCall()
    {
        const string text = "WaitKey(10000)";
        var call = Parsing.ParseExpression(text).Root as FunctionCallExpression;
        Assert.NotNull(call);
        Assert.Equal(SyntaxNodeKind.FunctionCallExpression, call.Kind);
        Assert.Equal("WaitKey", call.TargetName.Value);
        Assert.Single(call.Arguments);

        //Assert.Equal(text, call.ToString());
    }

    [Fact]
    public void ParseDeltaExpression()
    {
        const string text = "@100";
        var deltaExpr = Parsing.ParseExpression(text).Root as UnaryExpression;
        Assert.NotNull(deltaExpr);
        Assert.Equal(SyntaxNodeKind.UnaryExpression, deltaExpr.Kind);
        Assert.Equal(UnaryOperatorKind.Delta, deltaExpr.OperatorKind.Value);

        LiteralExpression operand = Assert.IsType<LiteralExpression>(deltaExpr.Operand);
        Assert.Equal(BuiltInType.Numeric, operand.Value.Type);
        Assert.Equal(100, operand.Value.AsNumber());
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
        const string text = "$a++";
        var expr = Parsing.ParseExpression(text).Root as AssignmentExpression;
        Assert.NotNull(expr);
        Assert.Equal(AssignmentOperatorKind.Increment, expr.OperatorKind.Value);
        Assert.Equal(expr.Target, expr.Value);
    }

    [Fact]
    public void ParseDecrement()
    {
        const string text = "$a--";
        var expr = Parsing.ParseExpression(text).Root as AssignmentExpression;
        Assert.NotNull(expr);
        Assert.Equal(AssignmentOperatorKind.Decrement, expr.OperatorKind.Value);
        Assert.Equal(expr.Target, expr.Value);
    }

    private static void TestUnary(UnaryOperatorKind kind)
    {
        string text = OperatorInfo.GetText(kind) + "$a";
        var expr = Parsing.ParseExpression(text).Root as UnaryExpression;

        Assert.NotNull(expr);
        Assert.Equal(SyntaxNodeKind.UnaryExpression, expr.Kind);
        Assert.Equal(kind, expr.OperatorKind.Value);

        var operand = expr.Operand as NameExpression;
        Assert.NotNull(operand);
        Assert.Equal("a", operand.Name);
    }

    private static void TestBinary(BinaryOperatorKind kind)
    {
        string text = "$a " + OperatorInfo.GetText(kind) + " $b";
        var expr = Parsing.ParseExpression(text).Root as BinaryExpression;

        Assert.NotNull(expr);
        Assert.Equal(SyntaxNodeKind.BinaryExpression, expr.Kind);
        Assert.Equal(kind, expr.OperatorKind.Value);

        var left = expr.Left as NameExpression;
        Assert.NotNull(left);
        Assert.Equal("a", left.Name);

        var right = expr.Right as NameExpression;
        Assert.NotNull(right);
        Assert.Equal("b", right.Name);
    }

    private static void TestAssignment(AssignmentOperatorKind kind)
    {
        string text = "$a " + OperatorInfo.GetText(kind) + " 42";
        var expr = Parsing.ParseExpression(text).Root as AssignmentExpression;

        Assert.NotNull(expr);
        Assert.Equal(SyntaxNodeKind.AssignmentExpression, expr.Kind);
        Assert.Equal(kind, expr.OperatorKind.Value);

        var target = expr.Target as NameExpression;
        Assert.NotNull(target);
        Assert.Equal("a", target.Name);

        var value = expr.Value as LiteralExpression;
        Assert.NotNull(value);
        //Assert.Equal(42.0d, value.Value.DoubleValue);
    }
}
