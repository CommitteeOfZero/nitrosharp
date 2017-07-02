namespace NitroSharp.NsScript.PXml
{
    public abstract class PXmlNode
    {
        public abstract PXmlNodeKind Kind { get; }
        internal abstract void Accept(PXmlSyntaxVisitor visitor);
    }
}
