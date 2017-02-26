using System.Collections.Generic;
using Xunit;

namespace SciAdvNet.NSScript.Tests
{
    public class ExpressionEvaluationTests
    {
        //[Fact]
        //public void TestBinaryExpressionEvaluation()
        //{
        //    var env = new Dictionary<string, object>();
        //    env["$a"] = 5;
        //    env["$b"] = 3;
        //    env["#flagA"] = true;
        //    env["#flagB"] = false;

        //    TestBinaryExpressionEvaluationImpl(env, "$a + $b", 8);
        //    TestBinaryExpressionEvaluationImpl(env, "$a - $b", 2);
        //    TestBinaryExpressionEvaluationImpl(env, "$a * $b", 15);
        //    TestBinaryExpressionEvaluationImpl(env, "$a / $b", 1);
        //    TestBinaryExpressionEvaluationImpl(env, "$a == $b", false);
        //    TestBinaryExpressionEvaluationImpl(env, "$a != $b", true);
        //    TestBinaryExpressionEvaluationImpl(env, "$a < $b", false);
        //    TestBinaryExpressionEvaluationImpl(env, "$a <= $b", false);
        //    TestBinaryExpressionEvaluationImpl(env, "$a > $b", true);
        //    TestBinaryExpressionEvaluationImpl(env, "$a >= $b", true);
        //    TestBinaryExpressionEvaluationImpl(env, "#flagA == #flagB", false);
        //    TestBinaryExpressionEvaluationImpl(env, "#flagA != #flagB", true);
        //    TestBinaryExpressionEvaluationImpl(env, "#flagA && #flagB", false);
        //    TestBinaryExpressionEvaluationImpl(env, "#flagA || #flagB", true);
        //}

        //private void TestBinaryExpressionEvaluationImpl(Dictionary<string, object> env, string expr, object expectedResult)
        //{
        //    var parsedExpr = NSScript.ParseExpression(expr);
        //    object result = new EvaluatingVisitor(env).Evaluate(parsedExpr);
        //    Assert.Equal(expectedResult, result);
        //}
    }
}
