namespace NitroSharp.NsScript.Syntax.PXml
{
    public sealed class PXmlText : PXmlNode
    {
        internal PXmlText(string text)
        {
            Text = text;
        }

        public string Text { get; }
        public override PXmlNodeKind Kind => PXmlNodeKind.Text;

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitText(this);
        }
    }
}
