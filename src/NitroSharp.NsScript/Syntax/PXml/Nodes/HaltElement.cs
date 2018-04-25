namespace NitroSharp.NsScript.Syntax.PXml
{
    public sealed class HaltElement : PXmlNode
    {
        public override PXmlNodeKind Kind => PXmlNodeKind.HaltElement;

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitHaltElement(this);
        }
    }
}
