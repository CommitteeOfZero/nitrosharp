using SciAdvNet.NSScript.Execution;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SciAdvNet.NSScript.Tests
{
    public class RpnTests
    {
        [Fact]
        public void SimpleExpr()
        {
            string expr = "$target = $a + (2 * 3) + $b;";
            var tree = NSScript.ParseExpression(expr);

            var conv = new ExpressionVisitor();
            conv.Visit(tree);
        }
    }
}
