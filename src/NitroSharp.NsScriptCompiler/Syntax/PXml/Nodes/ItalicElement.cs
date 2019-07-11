namespace NitroSharp.NsScript.Syntax.PXml
{
    public sealed class ItalicElement : PXmlNode
    {
        internal ItalicElement(PXmlContent content)
        {
            Content = content;
        }

        public PXmlContent Content { get; }

        public override PXmlNodeKind Kind => PXmlNodeKind.ItalicElement;

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitItalicElement(this);
        }
    }
}
