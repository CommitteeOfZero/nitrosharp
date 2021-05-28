using System;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax.Markup
{
    public class MarkupNodeVisitor
    {
        public void Visit(MarkupNode node)
        {
            node.Accept(this);
        }

        public void VisitArray(ImmutableArray<MarkupNode> list)
        {
            foreach (MarkupNode node in list)
            {
                Visit(node);
            }
        }

        public virtual void VisitContent(MarkupContent content)
        {
        }

        public virtual void VisitText(MarkupText text)
        {
        }

        public virtual void VisitVoiceElement(VoiceElement voiceElement)
        {
        }

        public virtual void VisitFontElement(FontElement fontElement)
        {
        }

        public virtual void VisitSpanElement(SpanElement spanElement)
        {
        }

        public virtual void VisitRubyElement(RubyElement rubyElement)
        {
        }

        public virtual void VisitHaltElement(HaltElement haltElement)
        {
        }

        public virtual void VisitNoLinebreaksElement(NoLinebreaksElement noLinebreaksElement)
        {
        }

        public virtual void VisitItalicElement(ItalicElement italicElement)
        {
        }

        public virtual void VisitLinebreakElement(LinebreakElement linebreakElement)
        {
        }
    }
}
