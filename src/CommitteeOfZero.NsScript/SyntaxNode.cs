using System.IO;

namespace CommitteeOfZero.NsScript
{
    public abstract class SyntaxNode
    {
        public abstract SyntaxNodeKind Kind { get; }
        public abstract void Accept(SyntaxVisitor visitor);
        public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);

        public override string ToString()
        {
            var sw = new StringWriter();
            var codeWriter = new DefaultCodeWriter(sw);
            codeWriter.WriteNode(this);

            return sw.ToString();
        }
    }
}
