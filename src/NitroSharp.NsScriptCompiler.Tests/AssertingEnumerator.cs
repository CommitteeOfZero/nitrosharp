using System;
using System.Collections.Generic;
using System.Linq;
using NitroSharp.NsScriptNew.Syntax;
using Xunit;

namespace NitroSharp.NsScriptCompiler.Tests
{
    internal class AssertingEnumerator
    {
        private readonly SyntaxNode _node;
        private IEnumerator<SyntaxNode> _enumerator;

        public AssertingEnumerator(SyntaxNode node)
        {
            _node = node;
            _enumerator = Flatten(node).GetEnumerator();
        }

        public void AssertNode(SyntaxNodeKind kind)
        {
            Assert.True(_enumerator.MoveNext());
            Assert.Equal(kind, _enumerator.Current.Kind);
            _enumerator.MoveNext();
        }

        private static IEnumerable<SyntaxNode> Flatten(SyntaxNode node)
        {
            var stack = new Stack<SyntaxNode>();
            stack.Push(node);

            while (stack.Count > 0)
            {
                SyntaxNode n = stack.Pop();
                yield return n;

                foreach (SyntaxNode child in n.GetChildren().ToArray().Reverse())
                {
                    stack.Push(child);
                }
            }
        }
    }
}
