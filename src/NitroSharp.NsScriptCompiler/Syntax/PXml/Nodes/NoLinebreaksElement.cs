namespace NitroSharp.NsScript.Syntax.PXml
{
    public sealed class NoLinebreaksElement : PXmlNode
    {
        public override PXmlNodeKind Kind => PXmlNodeKind.NoLinebreaksElement;

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitNoLinebreaksElement(this);
        }
    }
}
