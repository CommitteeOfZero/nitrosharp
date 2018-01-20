//using NitroSharp.NsScript.Execution;
//using NitroSharp.NsScript.Symbols;
//using Xunit;

//namespace NitroSharp.NsScript.Tests
//{
//    public class ExpressionEvaluationTests
//    {
//        private readonly MemorySpace _locals = new MemorySpace();
//        private readonly Binder _binder = new Binder();

//        [Fact]
//        public void EvaluateBinary()
//        {
//            var globals = new MemorySpace();
//            globals.Set("a", ConstantValue.Create(10));
//            globals.Set("b", ConstantValue.Create(5));
//            globals.Set("flagA", ConstantValue.True);
//            globals.Set("flagB", ConstantValue.False);

//            var evaluator = new ExpressionEvaluator(globals, new BuiltIns());

//            Test(evaluator, "$a + $b", ConstantValue.Create(15));
//            Test(evaluator, "$a - $b", ConstantValue.Create(5));
//            Test(evaluator, "$a * $b", ConstantValue.Create(50));
//            Test(evaluator, "$a / $b", ConstantValue.Create(2));
//            Test(evaluator, "$a == $b", ConstantValue.False);
//            Test(evaluator, "$a != $b", ConstantValue.True);
//            Test(evaluator, "$a < $b", ConstantValue.False);
//            Test(evaluator, "$a <= $b", ConstantValue.False);
//            Test(evaluator, "$a > $b", ConstantValue.True);
//            Test(evaluator, "$a >= $b", ConstantValue.True);
//            Test(evaluator, "#flagA == #flagB", ConstantValue.False);
//            Test(evaluator, "#flagA != #flagB", ConstantValue.True);
//            Test(evaluator, "#flagA && #flagB", ConstantValue.False);
//            Test(evaluator, "#flagA || #flagB", ConstantValue.True);

//            Test(evaluator, "10 % 3", ConstantValue.Create(1));
//        }

//        private void Test(ExpressionEvaluator evaluator, string expression, ConstantValue expectedResult)
//        {
//            var expr = Parsing.ParseExpression(expression);
//            _binder.Visit(expr);
//            ConstantValue result = evaluator.Evaluate(expr, _locals);
//            Assert.Equal(expectedResult, result);
//        }

//        private sealed class BuiltIns : EngineImplementationBase { }
//    }
//}
