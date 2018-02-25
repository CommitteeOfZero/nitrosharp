using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax.PXml
{
    public sealed class PXmlContent : PXmlNode
    {
        internal PXmlContent(ImmutableArray<PXmlNode> children)
        {
            Children = children;
        }

        public ImmutableArray<PXmlNode> Children { get; }

        public override PXmlNodeKind Kind => PXmlNodeKind.Content;

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitContent(this);
        }
    }
}
