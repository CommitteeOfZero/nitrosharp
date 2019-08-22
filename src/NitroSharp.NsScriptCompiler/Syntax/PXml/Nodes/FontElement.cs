namespace NitroSharp.NsScript.Syntax.PXml
{
    public sealed class FontElement : PXmlNode
    {
        internal FontElement(int? size, NsColor? color, NsColor? outlineColor, PXmlContent content)
        {
            Size = size;
            Color = color;
            OutlineColor = outlineColor;
            Content = content;
        }

        public int? Size { get; }
        public NsColor? Color { get; }
        public NsColor? OutlineColor { get; }
        public PXmlContent Content { get; }

        public override PXmlNodeKind Kind => PXmlNodeKind.FontElement;

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitFontElement(this);
        }
    }
}
