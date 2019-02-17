namespace NitroSharp.NsScript.Syntax.PXml
{
    public sealed class RubyElement : PXmlNode
    {
        internal RubyElement(PXmlContent rubyBase, string rubyText)
        {
            RubyBase = rubyBase;
            RubyText = rubyText;
        }

        public PXmlContent RubyBase { get; }
        public string RubyText { get; }

        public override PXmlNodeKind Kind => PXmlNodeKind.RubyElement;

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitRubyElement(this);
        }
    }
}
