namespace NitroSharp.NsScript.Syntax.PXml
{
    public sealed class ColorElement : PXmlNode
    {
        internal ColorElement(NsColor color, PXmlContent content)
        {
            Color = color;
            Content = content;
        }

        public NsColor Color { get; }
        public PXmlContent Content { get; }

        public override PXmlNodeKind Kind => PXmlNodeKind.ColorElement;

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitColorElement(this);
        }
    }
}
