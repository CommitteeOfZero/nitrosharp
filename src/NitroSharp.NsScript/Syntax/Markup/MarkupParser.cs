using NitroSharp.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace NitroSharp.NsScript.Syntax.Markup
{
    internal sealed class MarkupParser : TextScanner
    {
        public MarkupParser(string sourceText) : base(sourceText)
        {
        }

        public MarkupContent Parse()
        {
            return ParseContent(rootElementName: null);
        }

        private MarkupContent ParseContent(string? rootElementName)
        {
            var children = ImmutableArray.Create<MarkupNode>();
            ImmutableArray<MarkupNode>.Builder? builder = null;
            while (PeekChar() != EofCharacter)
            {
                if (IsEndTag())
                {
                    MarkupTag endTag = ParseTag();
                    if (rootElementName == endTag.Name)
                    {
                        break;
                    }
                }

                MarkupNode? node = ParseNode();
                if (node is null) { continue; }
                if (children.Length == 0)
                {
                    children = ImmutableArray.Create(node);
                }
                else
                {
                    Debug.Assert(children.Length == 1);
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<MarkupNode>();
                        builder.Add(children[0]);
                    }
                    builder.Add(node);
                }
            }

            ImmutableArray<MarkupNode> array = builder?.ToImmutable() ?? children;
            return new MarkupContent(array);
        }

        private MarkupNode? ParseNode()
        {
            SkipTrivia();
            StartScanning();
            char peek = PeekChar();
            return peek == '<' ? ParseElement() : ParsePlainText();
        }

        private MarkupNode? ParseElement()
        {
            MarkupTag startTag = ParseTag();
            MarkupNode? node;
            switch (startTag.Name)
            {
                case "FONT":
                    node = ParseFontElement(startTag);
                    break;
                case "span":
                case "SPAN":
                    node = ParseSpan(startTag);
                    break;
                case "RUBY":
                    node = ParseRubyElement(startTag);
                    break;
                case "pre":
                case "PRE":
                    node = ParsePreformattedText();
                    break;
                case "voice":
                    node = ParseVoiceElement(startTag);
                    break;
                case "k":
                case "K":
                    return new HaltElement();
                case "?":
                    return new NoLinebreaksElement();
                case "br":
                case "BR":
                    return new LinebreakElement();
                case "i":
                case "I":
                    MarkupContent content = ParseContent(startTag.Name);
                    return new ItalicElement(content);
                case "u":
                case "U":
                    return ParseContent(startTag.Name);
                default:
                    throw new NotImplementedException($"Markup tag '{startTag.Name}' is not yet supported.");
            }
            return node;
        }

        private SpanElement? ParseSpan(in MarkupTag tag)
        {
            MarkupContent content = ParseContent(tag.Name);
            AttributeList attrs = tag.Attributes;
            if (attrs.Get("value") is string strValue
                && int.TryParse(strValue, out int value))
            {
                return new SpanElement(value, content);
            }

            return null;
        }

        private VoiceElement? ParseVoiceElement(in MarkupTag tag)
        {
            AttributeList attrs = tag.Attributes;
            if (attrs.Get("name") is string characterName
                && attrs.Get("src") is string fileName)
            {
                bool stop = attrs.Get("mode") is "off";
                var action = stop ? NsVoiceAction.Stop : NsVoiceAction.Play;
                ScanEndOfLineSequence();
                return new VoiceElement(action, characterName, fileName);
            }

            return null;
        }

        private FontElement ParseFontElement(in MarkupTag startTag)
        {
            int? size = null;
            NsColor? color = null, outlineColor = null;

            AttributeList attributes = startTag.Attributes;
            if (attributes.Get("size") is string strSize)
            {
                size = int.Parse(strSize);
            }
            if (attributes.Get("incolor") is string strColor)
            {
                color = NsColor.FromString(strColor);
            }
            if (attributes.Get("outcolor") is string strOutlineColor)
            {
                outlineColor = NsColor.FromString(strOutlineColor);
            }

            MarkupContent content = ParseContent(startTag.Name);
            return new FontElement(size, color, outlineColor, content);
        }

        private RubyElement? ParseRubyElement(in MarkupTag startTag)
        {
            if (startTag.Attributes.Get("text") is string rubyText)
            {
                MarkupContent rubyBase = ParseContent("RUBY");
                return new RubyElement(rubyBase, rubyText);
            }

            return null;
        }

        private MarkupText ParsePreformattedText()
        {
            StartScanning();
            var sb = new StringBuilder();
            char c;
            while ((c = PeekChar()) != EofCharacter
                && !Match("</pre>") && !Match("</PRE>"))
            {
                AdvanceChar();
                sb.Append(c);
            }

            AdvanceChar(6);
            return new MarkupText(sb.ToString());
        }

        private MarkupText ParsePlainText()
        {
            StartScanning();
            var sb = new StringBuilder();

            char c;
            while ((c = PeekChar()) != '<' && c != EofCharacter)
            {
                char next = PeekChar(1);
                switch (c)
                {
                    case '/' when next == '/':
                        ScanToEndOfLine();
                        ScanEndOfLineSequence();
                        continue;
                    case '&' or '\\' when next is '.' or ',':
                        AdvanceChar(2);
                        sb.Append(next);
                        continue;
                    default:
                        sb.Append(c);
                        AdvanceChar();
                        break;
                }
            }

            string processedText = sb.ToString();
            return new MarkupText(processedText);
        }

        private bool IsEndTag() => PeekChar() == '<' && PeekChar(1) == '/';

        private void SkipTrivia()
        {
            StartScanning();
            bool trivia = true;
            do
            {
                char character = PeekChar();
                if (character == '/' && PeekChar(1) == '/')
                {
                    ScanToEndOfLine();
                    ScanEndOfLineSequence();
                }
                else
                {
                    trivia = false;
                }
            } while (trivia);
        }

        private MarkupTag ParseTag()
        {
            while (PeekChar() == '<' || PeekChar() == '/')
            {
                EatChar(PeekChar());
            }

            StartScanning();
            char c;
            while (!SyntaxFacts.IsWhitespace(c = PeekChar())
                && !SyntaxFacts.IsNewLine(c) && c != '>')
            {
                AdvanceChar();
            }

            TextSpan span = CurrentLexemeSpan;
            string name = Text.Substring(span.Start, span.Length);
            var attributes = new AttributeListBuilder();
            while ((c = PeekChar()) != '>')
            {
                if (c == ' ' || SyntaxFacts.IsNewLine(c))
                {
                    AdvanceChar();
                }
                else
                {
                    attributes.Add(ParseXmlAttribute());
                }
            }

            EatChar('>');
            return new MarkupTag(name, attributes.Build());
        }

        private (string key, string value) ParseXmlAttribute()
        {
            char c;
            StartScanning();
            while (PeekChar() != '=')
            {
                AdvanceChar();
            }
            TextSpan span = CurrentLexemeSpan;
            string key = Text.Substring(span.Start, span.Length);

            EatChar('=');
            TryEatChar('"');

            StartScanning();
            while ((c = PeekChar()) != '"' && c != '>' && c != ' ')
            {
                AdvanceChar();
            }
            span = CurrentLexemeSpan;
            string value = Text.Substring(span.Start, span.Length);
            TryEatChar('"');
            return (key, value);
        }

        private readonly ref struct MarkupTag
        {
            public MarkupTag(string name, AttributeList attributes)
            {
                Name = name;
                Attributes = attributes;
            }

            public string Name { get; }
            public AttributeList Attributes { get; }
        }

        private struct AttributeListBuilder
        {
            private SmallList<(string, string)> _attributes;

            public void Add((string key, string value) attribute)
            {
                if (Get(attribute.key) is not null)
                {
                    throw new Exception($"Attribute '{attribute.key}' specified more than once.");
                }
                _attributes.Add(attribute);
            }

            public AttributeList Build()
            {
                var list = new AttributeList(_attributes);
                _attributes = default;
                return list;
            }

            private string? Get(string key)
                => AttributeList.Get(_attributes, key);
        }

        private readonly ref struct AttributeList
        {
            private readonly SmallList<(string, string)> _attributes;

            public AttributeList(SmallList<(string, string)> attributes)
                => _attributes = attributes;

            public string? Get(string key)
                => Get(_attributes, key);

            public static string? Get(SmallList<(string, string)> list, string key)
            {
                foreach ((string k, string v) in list.AsSpan())
                {
                    if (k.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        return v;
                    }
                }

                return null;
            }
        }
    }
}
