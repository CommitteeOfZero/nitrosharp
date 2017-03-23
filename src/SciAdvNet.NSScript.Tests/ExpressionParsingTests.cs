using Xunit;

namespace SciAdvNet.NSScript.Tests
{
    public class ExpressionParsingTests
    {
        [Fact]
        public void TestNumericLiteralExpression()
        {
            string literal = "42";
            var expr = NSScript.ParseExpression(literal) as Literal;

            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.Literal, expr.Kind);
            Assert.Equal(literal, expr.Text);
            Assert.Equal(42, expr.Value.RawValue);
            Assert.Equal(literal, expr.ToString());
        }

        [Fact]
        public void TestStringLiteralExpression()
        {
            string literal = "\"stuff\"";
            var expr = NSScript.ParseExpression(literal) as Literal;

            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.Literal, expr.Kind);
            Assert.Equal(literal, expr.Text);
            Assert.Equal("stuff", expr.Value.RawValue);
            Assert.Equal(literal, expr.ToString());
        }

        [Fact]
        public void TestNamedConstant()
        {
            string text = "center";
            var constant = NSScript.ParseExpression(text) as NamedConstant;

            Assert.NotNull(constant);
            Assert.Equal(SyntaxNodeKind.NamedConstant, constant.Kind);
            Assert.Equal(text, constant.Name.FullName);
            Assert.Equal(text, constant.ToString());
        }

        [Fact]
        public void TestVariableWithDollarSigil()
        {
            string text = "$testVar";
            var variable = NSScript.ParseExpression(text) as Variable;

            Assert.NotNull(variable);
            Assert.Equal(SyntaxNodeKind.Variable, variable.Kind);
            Assert.Equal(text, variable.Name.FullName);
            Assert.Equal("testVar", variable.Name.SimplifiedName);
            Assert.Equal(SigilKind.Dollar, variable.Name.Sigil);
            Assert.Equal(text, variable.ToString());
        }

        [Fact]
        public void TestVariableWithHashSigil()
        {
            string text = "#testVar";
            var variable = NSScript.ParseExpression(text) as Variable;

            Assert.NotNull(variable);
            Assert.Equal(SyntaxNodeKind.Variable, variable.Kind);
            Assert.Equal(text, variable.Name.FullName);
            Assert.Equal("testVar", variable.Name.SimplifiedName);
            Assert.Equal(SigilKind.Hash, variable.Name.Sigil);
            Assert.Equal(text, variable.ToString());
        }

        [Fact]
        public void TestVariableInQuotes()
        {
            string text = "\"$testVar\"";
            var variable = NSScript.ParseExpression(text) as Variable;

            Assert.NotNull(variable);
            Assert.Equal(SyntaxNodeKind.Variable, variable.Kind);
            Assert.Equal(text, variable.Name.FullName);
            Assert.Equal("testVar", variable.Name.SimplifiedName);
            Assert.Equal(SigilKind.Dollar, variable.Name.Sigil);
            Assert.Equal(text, variable.ToString());
        }

        [Fact]
        public void TestUnaryOperators()
        {
            TestUnary(OperationKind.LogicalNegation);
            TestUnary(OperationKind.PostfixDecrement);
            TestUnary(OperationKind.PostfixIncrement);
            TestUnary(OperationKind.UnaryMinus);
            TestUnary(OperationKind.UnaryPlus);
        }

        [Fact]
        public void TestBinaryOperators()
        {
            TestBinary(OperationKind.Addition);
            TestBinary(OperationKind.Division);
            TestBinary(OperationKind.Equal);
            TestBinary(OperationKind.GreaterThan);
            TestBinary(OperationKind.GreaterThanOrEqual);
            TestBinary(OperationKind.LessThan);
            TestBinary(OperationKind.LessThanOrEqual);
            TestBinary(OperationKind.LogicalAnd);
            TestBinary(OperationKind.LogicalOr);
            TestBinary(OperationKind.Multiplication);
            TestBinary(OperationKind.NotEqual);
            TestBinary(OperationKind.Subtraction);
        }

        [Fact]
        public void TestAssignmentOperators()
        {
            TestAssignment(OperationKind.AddAssignment);
            TestAssignment(OperationKind.DivideAssignment);
            TestAssignment(OperationKind.MultiplyAssignment);
            TestAssignment(OperationKind.SimpleAssignment);
            TestAssignment(OperationKind.SubtractAssignment);
        }

        private void TestUnary(OperationKind kind)
        {
            string text;
            if (Operation.IsPrefixOperation(kind))
            {
                text = Operation.GetText(kind) + "$a";
            }
            else
            {
                text = "$a" + Operation.GetText(kind);
            }

            var expr = NSScript.ParseExpression(text) as UnaryExpression;

            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.UnaryExpression, expr.Kind);
            Assert.Equal(kind, expr.OperationKind);

            var operand = expr.Operand as Variable;
            Assert.NotNull(operand);
            Assert.Equal("$a", operand.Name.FullName);

            Assert.Equal(text, expr.ToString());
        }

        private void TestBinary(OperationKind kind)
        {
            string text = "$a " + Operation.GetText(kind) + " $b";
            var expr = NSScript.ParseExpression(text) as BinaryExpression;

            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.BinaryExpression, expr.Kind);
            Assert.Equal(kind, expr.OperationKind);

            var left = expr.Left as Variable;
            Assert.NotNull(left);
            Assert.Equal("$a", left.Name.FullName);

            var right = expr.Right as Variable;
            Assert.NotNull(right);
            Assert.Equal("$b", right.Name.FullName);

            Assert.Equal(text, expr.ToString());
        }

        private void TestAssignment(OperationKind kind)
        {
            string text = "$a " + Operation.GetText(kind) + " 42";
            var expr = NSScript.ParseExpression(text) as AssignmentExpression;

            Assert.NotNull(expr);
            Assert.Equal(SyntaxNodeKind.AssignmentExpression, expr.Kind);
            Assert.Equal(kind, expr.OperationKind);

            var target = expr.Target as Variable;
            Assert.NotNull(target);
            Assert.Equal("$a", target.Name.FullName);

            var value = expr.Value as Literal;
            Assert.NotNull(value);
            Assert.Equal(42, value.Value.RawValue);
        }

        [Fact]
        public void TestVariableReference()
        {
            string text = "SomeMethod($a);";
            var invocation = NSScript.ParseStatement(text) as MethodCall;

            var arg = invocation.Arguments[0] as Variable;
            Assert.NotNull(arg);
            Assert.Equal(SyntaxNodeKind.Variable, arg.Kind);
        }

        [Fact]
        public void TestIntParameterReference()
        {
            string text = "function Test(intParam) { SomeMethod(intParam); }";
            var root = NSScript.ParseScript(text);
            var method = root.Methods[0];

            var invocation = method.Body.Statements[0] as MethodCall;
            Assert.NotNull(invocation);

            var arg = invocation.Arguments[0] as ParameterReference;
            Assert.NotNull(arg);
            Assert.Equal(SyntaxNodeKind.Parameter, arg.Kind);
            Assert.Equal("intParam", arg.ParameterName.FullName);
            Assert.Equal(arg.ParameterName.FullName, arg.ParameterName.SimplifiedName);
            Assert.Equal(SigilKind.None, arg.ParameterName.Sigil);
        }

        [Fact]
        public void TestIntParameterReferenceWithSigil()
        {
            TestIntParameterReferenceWithSigilImpl("$intParam", "intParam", SigilKind.Dollar);
            TestIntParameterReferenceWithSigilImpl("#intParam", "intParam", SigilKind.Hash);
        }

        private void TestIntParameterReferenceWithSigilImpl(string fullName, string simplifiedName, SigilKind sigil)
        {
            string text = $"function Test({fullName}) {{ SomeMethod({fullName}); }}";
            var root = NSScript.ParseScript(text);
            var method = root.Methods[0];

            var invocation = method.Body.Statements[0] as MethodCall;
            Assert.NotNull(invocation);
            var arg = invocation.Arguments[0] as ParameterReference;
            Assert.NotNull(arg);
            Assert.Equal(SyntaxNodeKind.Parameter, arg.Kind);
            Assert.Equal(fullName, arg.ParameterName.FullName);
            Assert.Equal(simplifiedName, arg.ParameterName.SimplifiedName);
            Assert.Equal(sigil, arg.ParameterName.Sigil);
        }

        [Fact]
        public void TestStringParameterReference()
        {
            string text = "function Test(\"stringParam\") { SomeMethod(\"stringParam\"); }";
            var root = NSScript.ParseScript(text);
            var method = root.Methods[0];

            var invocation = method.Body.Statements[0] as MethodCall;
            Assert.NotNull(invocation);
            var arg = invocation.Arguments[0] as ParameterReference;
            Assert.NotNull(arg);
            Assert.Equal(SyntaxNodeKind.Parameter, arg.Kind);
            Assert.Equal("\"stringParam\"", arg.ParameterName.FullName);
            Assert.Equal("stringParam", arg.ParameterName.SimplifiedName);
            Assert.Equal(SigilKind.None, arg.ParameterName.Sigil);
        }

        [Fact]
        public void TestStringParameterReferenceWithSigil()
        {
            TestStringParameterReferenceWithSigilImpl("\"$stringParam\"", "stringParam", SigilKind.Dollar);
            TestStringParameterReferenceWithSigilImpl("\"#stringParam\"", "stringParam", SigilKind.Hash);
        }

        private void TestStringParameterReferenceWithSigilImpl(string fullName, string simplifiedName, SigilKind sigil)
        {
            string text = $"function Test({fullName}) {{ SomeMethod({fullName}); }}";
            var root = NSScript.ParseScript(text);
            var method = root.Methods[0];

            var invocation = method.Body.Statements[0] as MethodCall;
            Assert.NotNull(invocation);
            var arg = invocation.Arguments[0] as ParameterReference;
            Assert.NotNull(arg);
            Assert.Equal(SyntaxNodeKind.Parameter, arg.Kind);
            Assert.Equal(fullName, arg.ParameterName.FullName);
            Assert.Equal(simplifiedName, arg.ParameterName.SimplifiedName);
            Assert.Equal(sigil, arg.ParameterName.Sigil);
        }
    }
}
