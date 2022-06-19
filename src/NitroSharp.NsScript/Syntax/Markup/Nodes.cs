using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax.Markup
{
    public enum MarkupNodeKind
    {
        Content,
        Text,
        VoiceElement,
        FontElement,
        SpanElement,
        RubyElement,
        HaltElement,
        NoLinebreaksElement,
        ItalicElement,
        LinebreakElement
    }

    public abstract class MarkupNode
    {
        public abstract MarkupNodeKind Kind { get; }
        internal abstract void Accept(MarkupNodeVisitor visitor);
    }

    public sealed class MarkupContent : MarkupNode
    {
        internal MarkupContent(ImmutableArray<MarkupNode> children)
        {
            Children = children;
        }

        public ImmutableArray<MarkupNode> Children { get; }

        public override MarkupNodeKind Kind => MarkupNodeKind.Content;

        internal override void Accept(MarkupNodeVisitor visitor)
        {
            visitor.VisitContent(this);
        }
    }

    public sealed class MarkupText : MarkupNode
    {
        internal MarkupText(string text)
        {
            Text = text;
        }

        public string Text { get; }
        public override MarkupNodeKind Kind => MarkupNodeKind.Text;

        internal override void Accept(MarkupNodeVisitor visitor)
        {
            visitor.VisitText(this);
        }
    }

     public sealed class VoiceElement : MarkupNode
    {
        internal VoiceElement(NsVoiceAction action, string characterName, string fileName)
        {
            Action = action;
            CharacterName = characterName;
            FileName = fileName;
        }

        public override MarkupNodeKind Kind => MarkupNodeKind.VoiceElement;
        public NsVoiceAction Action { get; }
        public string CharacterName { get; }
        public string FileName { get; }

        internal override void Accept(MarkupNodeVisitor visitor)
        {
            visitor.VisitVoiceElement(this);
        }
    }

    public sealed class FontElement : MarkupNode
    {
        internal FontElement(uint? size, NsColor? color, NsColor? outlineColor, MarkupContent content)
        {
            Size = size;
            Color = color;
            OutlineColor = outlineColor;
            Content = content;
        }

        public uint? Size { get; }
        public NsColor? Color { get; }
        public NsColor? OutlineColor { get; }
        public MarkupContent Content { get; }

        public override MarkupNodeKind Kind => MarkupNodeKind.FontElement;

        internal override void Accept(MarkupNodeVisitor visitor)
        {
            visitor.VisitFontElement(this);
        }
    }

    public sealed class SpanElement : MarkupNode
    {
        public uint Size { get; }
        public MarkupContent Content { get; }

        public SpanElement(uint size, MarkupContent content)
        {
            Size = size;
            Content = content;
        }

        public override MarkupNodeKind Kind => MarkupNodeKind.SpanElement;

        internal override void Accept(MarkupNodeVisitor visitor)
        {
            visitor.VisitSpanElement(this);
        }
    }

    public sealed class RubyElement : MarkupNode
    {
        internal RubyElement(MarkupContent rubyBase, string rubyText)
        {
            RubyBase = rubyBase;
            RubyText = rubyText;
        }

        public MarkupContent RubyBase { get; }
        public string RubyText { get; }

        public override MarkupNodeKind Kind => MarkupNodeKind.RubyElement;

        internal override void Accept(MarkupNodeVisitor visitor)
        {
            visitor.VisitRubyElement(this);
        }
    }

    public sealed class HaltElement : MarkupNode
    {
        public override MarkupNodeKind Kind => MarkupNodeKind.HaltElement;

        internal override void Accept(MarkupNodeVisitor visitor)
        {
            visitor.VisitHaltElement(this);
        }
    }

    public sealed class LinebreakElement : MarkupNode
    {
        public override MarkupNodeKind Kind => MarkupNodeKind.LinebreakElement;

        internal override void Accept(MarkupNodeVisitor visitor)
        {
            visitor.VisitLinebreakElement(this);
        }
    }

    public sealed class NoLinebreaksElement : MarkupNode
    {
        public override MarkupNodeKind Kind => MarkupNodeKind.NoLinebreaksElement;

        internal override void Accept(MarkupNodeVisitor visitor)
        {
            visitor.VisitNoLinebreaksElement(this);
        }
    }

    public sealed class ItalicElement : MarkupNode
    {
        internal ItalicElement(MarkupContent content)
        {
            Content = content;
        }

        public MarkupContent Content { get; }

        public override MarkupNodeKind Kind => MarkupNodeKind.ItalicElement;

        internal override void Accept(MarkupNodeVisitor visitor)
        {
            visitor.VisitItalicElement(this);
        }
    }
}
