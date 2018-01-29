using NitroSharp.NsScript.Syntax;
using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class ExpressionParsingTests
    {
        [Fact]
        public void ParseFunctionCall()
        {
            string text = "WaitKey(10000)";
            var call = Parsing.ParseExpression(text) as FunctionCall;
            Assert.NotNull(call);
            Assert.Equal(SyntaxNodeKind.FunctionCall, call.Kind);
            Assert.Equal("WaitKey", call.Target.Name);
            Assert.Equal(SigilKind.None, call.Target.Sigil);
            Assert.Single(call.Arguments);

            Assert.Equal(text, call.ToString());
        }

        [Fact]
        public void ParseDeltaExpression()
        {
            string text = "@100";
            var deltaExpr = Parsing.ParseExpression(text) as DeltaExpression;

            Assert.NotNull(deltaExpr);
            Assert.Equal(SyntaxNodeKind.DeltaExpression, deltaExpr.Kind);
            Assert.NotNull(deltaExpr.Expression);
            Assert.Equal(text, deltaExpr.ToString());
        }

        [Fact]
        public void ParseNumericLiteralExpression()
        {
            string literal = "42";
            var expr = Parsing.ParseExpression(literal) as Literal;

            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.Literal, expr.Kind);
            Assert.Equal(literal, expr.Text);
            Assert.Equal(42.0d, expr.Value.DoubleValue);
            Assert.Equal(literal, expr.ToString());
        }

        [Fact]
        public void ParseStringLiteralExpression()
        {
            string literal = "\"stuff\"";
            var expr = Parsing.ParseExpression(literal) as Literal;

            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.Literal, expr.Kind);
            Assert.Equal(literal, expr.Text);
            Assert.Equal("stuff", expr.Value.StringValue);
            Assert.Equal(literal, expr.ToString());
        }

        [Fact]
        public void ParseIdentifierWithoutSigil()
        {
            string text = "foo";
            var identifier = Parsing.ParseExpression(text) as Identifier;

            Assert.NotNull(identifier);
            Assert.Equal(text, identifier.Name);
            Assert.Equal(SigilKind.None, identifier.Sigil);
            Assert.Equal(text, identifier.ToString());
        }

        [Fact]
        public void ParseIdentifierWithDollarSigil()
        {
            string text = "$foo";
            var identifier = Parsing.ParseExpression(text) as Identifier;

            Assert.NotNull(identifier);
            Assert.Equal("foo", identifier.Name);
            Assert.Equal(SigilKind.Dollar, identifier.Sigil);
            Assert.True(identifier.HasSigil);
            Assert.False(identifier.IsQuoted);
            Assert.Equal(text, identifier.ToString());
        }

        [Fact]
        public void ParseIdentifierWithHashSigil()
        {
            string text = "#foo";
            var identifier = Parsing.ParseExpression(text) as Identifier;

            Assert.NotNull(identifier);
            Assert.Equal("foo", identifier.Name);
            Assert.Equal(SigilKind.Hash, identifier.Sigil);
            Assert.True(identifier.HasSigil);
            Assert.False(identifier.IsQuoted);
            Assert.Equal(text, identifier.ToString());
        }

        [Fact]
        public void ParseQuotedIdentifierWithoutSigil()
        {
            // Normally, "foo" would be considered a string literal.
            // However, if there's a string parameter named "foo" in the current scope,
            // every instance of "foo" in this scope is treated as an identifier.

            string text = "function Test(\"foo\") { SomeMethod(\"foo\"); }";
            var root = Parsing.ParseScript(text);
            var function = (Function)root.Members[0];

            var invocation = (function.Body.Statements[0] as ExpressionStatement)?.Expression as FunctionCall;
            Assert.NotNull(invocation);
            var arg = invocation.Arguments[0] as Identifier;
            Assert.NotNull(arg);
            Assert.Equal("foo", arg.Name);
            Assert.Equal(SigilKind.None, arg.Sigil);
            Assert.True(arg.IsQuoted);
        }

        [Fact]
        public void ParseQuotedIdentifierWithSigil()
        {
            string text = "\"$foo\"";
            var identifier = Parsing.ParseExpression(text) as Identifier;

            Assert.NotNull(identifier);
            Assert.Equal("foo", identifier.Name);
            Assert.Equal(SigilKind.Dollar, identifier.Sigil);
            Assert.True(identifier.HasSigil);
            Assert.True(identifier.IsQuoted);
            Assert.Equal(text, identifier.ToString());
        }

        [Fact]
        public void ParseQuotedUppercaseNullKeyword()
        {
            string text = "\"NULL\"";
            var expr = Parsing.ParseExpression(text) as Literal;

            Assert.NotNull(expr);
            Assert.Equal("null", expr.Text);
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
            var expr = Parsing.ParseExpression(text) as AssignmentExpression;
            Assert.NotNull(expr);
            Assert.Equal(AssignmentOperatorKind.Increment, expr.OperatorKind);
            Assert.Equal(expr.Target, expr.Value);
            Assert.Equal(text, expr.ToString());
        }
        
        [Fact]
        public void ParseDecrement()
        {
            string text = "$a--";
            var expr = Parsing.ParseExpression(text) as AssignmentExpression;
            Assert.NotNull(expr);
            Assert.Equal(AssignmentOperatorKind.Decrement, expr.OperatorKind);
            Assert.Equal(expr.Target, expr.Value);
            Assert.Equal(text, expr.ToString());
        }

        private void TestUnary(UnaryOperatorKind kind)
        {
            string text = OperatorInfo.GetText(kind) + "$a";
            var expr = Parsing.ParseExpression(text) as UnaryExpression;

            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.UnaryExpression, expr.Kind);
            Assert.Equal(kind, expr.OperatorKind);

            var operand = expr.Operand as Identifier;
            Assert.NotNull(operand);
            Assert.Equal("a", operand.Name);

            Assert.Equal(text, expr.ToString());
        }

        private void TestBinary(BinaryOperatorKind kind)
        {
            string text = "$a " + OperatorInfo.GetText(kind) + " $b";
            var expr = Parsing.ParseExpression(text) as BinaryExpression;

            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.BinaryExpression, expr.Kind);
            Assert.Equal(kind, expr.OperatorKind);

            var left = expr.Left as Identifier;
            Assert.NotNull(left);
            Assert.Equal("a", left.Name);

            var right = expr.Right as Identifier;
            Assert.NotNull(right);
            Assert.Equal("b", right.Name);

            Assert.Equal(text, expr.ToString());
        }

        private void TestAssignment(AssignmentOperatorKind kind)
        {
            string text = "$a " + OperatorInfo.GetText(kind) + " 42";
            var expr = Parsing.ParseExpression(text) as AssignmentExpression;

            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.AssignmentExpression, expr.Kind);
            Assert.Equal(kind, expr.OperatorKind);

            var target = expr.Target;
            Assert.NotNull(target);
            Assert.Equal("a", target.Name);

            var value = expr.Value as Literal;
            Assert.NotNull(value);
            Assert.Equal(42.0d, value.Value.DoubleValue);
            Assert.Equal(text, expr.ToString());
        }
    }
}
