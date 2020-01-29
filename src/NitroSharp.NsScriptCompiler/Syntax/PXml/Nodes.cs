using System.Collections.Immutable;

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

     public sealed class VoiceElement : PXmlNode
    {
        internal VoiceElement(NsVoiceAction action, string characterName, string fileName)
        {
            Action = action;
            CharacterName = characterName;
            FileName = fileName;
        }

        public override PXmlNodeKind Kind => PXmlNodeKind.VoiceElement;
        public NsVoiceAction Action { get; }
        public string CharacterName { get; }
        public string FileName { get; }

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitVoiceElement(this);
        }
    }

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

    public sealed class HaltElement : PXmlNode
    {
        public override PXmlNodeKind Kind => PXmlNodeKind.HaltElement;

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitHaltElement(this);
        }
    }

    public sealed class NoLinebreaksElement : PXmlNode
    {
        public override PXmlNodeKind Kind => PXmlNodeKind.NoLinebreaksElement;

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitNoLinebreaksElement(this);
        }
    }

    public sealed class ItalicElement : PXmlNode
    {
        internal ItalicElement(PXmlContent content)
        {
            Content = content;
        }

        public PXmlContent Content { get; }

        public override PXmlNodeKind Kind => PXmlNodeKind.ItalicElement;

        internal override void Accept(PXmlSyntaxVisitor visitor)
        {
            visitor.VisitItalicElement(this);
        }
    }
}
