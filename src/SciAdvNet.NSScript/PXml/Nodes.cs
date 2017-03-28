using System;
using System.Collections.Immutable;

namespace SciAdvNet.NSScript.PXml
{
    public sealed class PXmlContent : PXmlNode
    {
        internal PXmlContent(ImmutableArray<PXmlNode> children)
        {
            Children = children;
        }

        public ImmutableArray<PXmlNode> Children { get; }

        public override PXmlNodeKind Kind => PXmlNodeKind.Content;

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitContent(this);
        }
    }

    public class PXmlText : PXmlNode
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

    public sealed class VoiceElement : PXmlNode
    {
        internal VoiceElement(VoiceAction action, string characterName, string fileName)
        {
            Action = action;
            CharacterName = characterName;
            FileName = fileName;
        }

        public override PXmlNodeKind Kind => PXmlNodeKind.VoiceElement;
        public VoiceAction Action { get; }
        public string CharacterName { get; }
        public string FileName { get; }

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitVoiceElement(this);
        }
    }

    public sealed class FontColorElement : PXmlNode
    {
        internal FontColorElement(NssColor color, PXmlContent content)
        {
            Color = color;
            Content = content;
        }

        public NssColor Color { get; }
        public PXmlContent Content { get; }

        public override PXmlNodeKind Kind => PXmlNodeKind.FontColorElement;

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitFontColorElement(this);
        }
    }

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

    public sealed class PXmlVerbatimText : PXmlText
    {
        internal PXmlVerbatimText(string text)
            : base(text)
        {
        }
    }
}
