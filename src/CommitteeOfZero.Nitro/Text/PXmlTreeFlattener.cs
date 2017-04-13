using CommitteeOfZero.NsScript.PXml;
using System.Text;

namespace CommitteeOfZero.Nitro.Text
{
    public class PXmlTreeFlattener : PXmlSyntaxVisitor
    {
        private readonly StringBuilder _builder;

        public PXmlTreeFlattener()
        {
            _builder = new StringBuilder();
        }

        public string Flatten(PXmlContent treeRoot)
        {
            _builder.Clear();
            Visit(treeRoot);

            return _builder.ToString();
        }

        public override void VisitContent(PXmlContent content)
        {
            VisitArray(content.Children);
        }

        public override void VisitFontColorElement(FontColorElement fontColorElement)
        {
            Visit(fontColorElement.Content);
        }

        public override void VisitText(PXmlText text)
        {
            _builder.Append(text.Text);
        }
    }
}
