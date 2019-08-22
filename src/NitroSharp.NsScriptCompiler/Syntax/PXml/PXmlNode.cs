namespace NitroSharp.NsScript.Syntax.PXml
{
    public enum PXmlNodeKind
    {
        Content,
        Text,
        VoiceElement,
        FontElement,
        RubyElement,
        HaltElement,
        NoLinebreaksElement,
        ItalicElement
    }

    public abstract class PXmlNode
    {
        public abstract PXmlNodeKind Kind { get; }
        internal abstract void Accept(PXmlSyntaxVisitor visitor);
    }
}
