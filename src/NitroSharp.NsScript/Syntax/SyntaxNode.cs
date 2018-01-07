using NitroSharp.NsScript.Symbols;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NitroSharp.NsScript.Syntax
{
    public abstract class SyntaxNode
    {
        public abstract SyntaxNodeKind Kind { get; }

        /// <summary>
        /// The symbol this syntax node is bound to. Initialized during the binding phase.
        /// </summary>
        public Symbol Symbol { get; internal set; }

        public abstract void Accept(SyntaxVisitor visitor);
        public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);
        IEnumerable<SyntaxNode> Descendants() { return Enumerable.Empty<SyntaxNode>(); }

        public override string ToString()
        {
            var sw = new StringWriter();
            var codeWriter = new DefaultCodeWriter(sw);
            codeWriter.WriteNode(this);

            return sw.ToString();
        }
    }
}
