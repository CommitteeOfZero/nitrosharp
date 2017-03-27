using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace SciAdvNet.NSScript.PXml
{
    public sealed class PXmlContent
    {
        internal PXmlContent(ImmutableArray<PXmlNode> children)
        {
            Children = children;
        }

        public ImmutableArray<PXmlNode> Children { get; }
    }

    public sealed class VoiceElement
    {
        internal VoiceElement(string characterName, string fileName)
        {
            CharacterName = characterName;
            FileName = fileName;
        }

        public string CharacterName { get; }
        public string FileName { get; }
    }

    public sealed class ColorElement : PXmlNode
    {
        public ColorElement(int colorCode, PXmlContent content)
        {
            ColorCode = colorCode;
            Content = content;
        }

        public int ColorCode { get; }
        public PXmlContent Content { get; }
    }

    public sealed class RubyElement : PXmlNode
    {
        public RubyElement(PXmlContent rubyBase, string rubyText)
        {
            RubyBase = rubyBase;
            RubyText = rubyText;
        }

        public PXmlContent RubyBase { get; }
        public string RubyText { get; }
    }

    public class PXmlText : PXmlNode
    {
        internal PXmlText(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    public sealed class PXmlVerbatimText : PXmlText
    {
        public PXmlVerbatimText(string text)
            : base(text)
        {
        }
    }
}
