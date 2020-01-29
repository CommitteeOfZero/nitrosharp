using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using NitroSharp.NsScript.Text;

namespace NitroSharp.NsScript.Syntax.PXml
{
    internal sealed class PXmlParser : TextScanner
    {
        public PXmlParser(string sourceText) : base(sourceText)
        {
        }

        public PXmlContent Parse()
        {
            return ParseContent(rootElementName: null);
        }

        private PXmlContent ParseContent(string? rootElementName)
        {
            var children = ImmutableArray.Create<PXmlNode>();
            ImmutableArray<PXmlNode>.Builder? builder = null;
            while (PeekChar() != EofCharacter)
            {
                if (IsEndTag())
                {
                    PXmlTag endTag = ParsePXmlTag();
                    if (rootElementName == endTag.Name)
                    {
                        break;
                    }
                }

                PXmlNode node = ParseNode();
                Debug.Assert(node != null);
                if (children.Length == 0)
                {
                    children = ImmutableArray.Create(node);
                }
                else
                {
                    Debug.Assert(children.Length == 1);
                    if (builder == null)
                    {
                        builder = ImmutableArray.CreateBuilder<PXmlNode>();
                        builder.Add(children[0]);
                    }
                    builder.Add(node);
                }
            }

            ImmutableArray<PXmlNode> array = builder != null
                ? builder.ToImmutable()
                : children;
            return new PXmlContent(array);
        }

        private PXmlNode ParseNode()
        {
            SkipTrivia();
            StartScanning();
            char peek = PeekChar();
            return peek == '<' ? ParseElement() : ParsePlainText();
        }

        private PXmlNode ParseElement()
        {
            PXmlTag startTag = ParsePXmlTag();
            PXmlNode node;
            switch (startTag.Name)
            {
                case "FONT":
                    node = ParseFontElement(startTag);
                    break;

                case "RUBY":
                    node = ParseRubyElement(startTag);
                    break;

                case "pre":
                case "PRE":
                    node = ParsePlainText();
                    ParsePXmlTag();
                    break;

                case "voice":
                    node = ParseVoiceElement(startTag);
                    break;

                case "k":
                case "K":
                    return new HaltElement();
                case "?":
                    return new NoLinebreaksElement();

                case "i":
                case "I":
                    PXmlContent content = ParseContent(startTag.Name);
                    return new ItalicElement(content);

                default:
                    throw new NotImplementedException($"PXml tag '{startTag.Name}' is not yet supported.");
            }

            return node;
        }

        private VoiceElement ParseVoiceElement(in PXmlTag tag)
        {
            string characterName = tag.Attributes["name"];
            string fileName = tag.Attributes["src"];
            tag.Attributes.TryGetValue("mode", out string mode);
            bool stop = mode == "off";
            var action = stop ? NsVoiceAction.Stop : NsVoiceAction.Play;
            return new VoiceElement(action, characterName, fileName);
        }

        private FontElement ParseFontElement(in PXmlTag startTag)
        {
            int? size = null;
            NsColor? color = null, outlineColor = null;

            ImmutableDictionary<string, string> attributes = startTag.Attributes;
            if (attributes.TryGetValue("size", out string value))
            {
                size = int.Parse(value);
            }
            if (attributes.TryGetValue("incolor", out value))
            {
                color = NsColor.FromString(value);
            }
            if (attributes.TryGetValue("outcolor", out value))
            {
                outlineColor = NsColor.FromString(value);
            }

            PXmlContent content = ParseContent(startTag.Name);
            return new FontElement(size, color, outlineColor, content);
        }

        private RubyElement ParseRubyElement(in PXmlTag startTag)
        {
            string rubyText = startTag.Attributes["text"];
            PXmlContent rubyBase = ParseContent("RUBY");
            return new RubyElement(rubyBase, rubyText);
        }

        private PXmlText ParsePlainText()
        {
            StartScanning();
            var sb = new StringBuilder();

            char c;
            while ((c = PeekChar()) != '<' && c != EofCharacter)
            {
                char next = PeekChar(1);
                if (c == '/' && next == '/')
                {
                    ScanToEndOfLine();
                    ScanEndOfLineSequence();
                    continue;
                }

                if (c == '&' && (next == '.' || next == ','))
                {
                    AdvanceChar(2);
                    sb.Append(next);
                    continue;
                }

                sb.Append(c);
                AdvanceChar();
            }

            string processedText = sb.ToString();
            return new PXmlText(processedText);
        }

        private bool IsEndTag() => PeekChar() == '<' && PeekChar(1) == '/';

        private void SkipTrivia()
        {
            StartScanning();
            bool trivia = true;
            do
            {
                char character = PeekChar();
                if (SyntaxFacts.IsNewLine(character))
                {
                    ScanEndOfLine();
                    continue;
                }

                if (character == '/' && PeekChar(1) == '/')
                {
                    ScanToEndOfLine();
                }
                else
                {
                    trivia = false;
                }
            } while (trivia);
        }

        private PXmlTag ParsePXmlTag()
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
            Dictionary<string, string>? attributes = null;
            while ((c = PeekChar()) != '>')
            {
                if (c == ' ' || SyntaxFacts.IsNewLine(c))
                {
                    AdvanceChar();
                }
                else
                {
                    attributes ??= new Dictionary<string, string>();
                    KeyValuePair<string, string> kvp = ParseXmlAttribute();
                    attributes.Add(kvp.Key, kvp.Value);
                }
            }

            EatChar('>');
            ImmutableDictionary<string, string> attr = attributes != null
                ? attributes.ToImmutableDictionary()
                : ImmutableDictionary<string, string>.Empty;
            return new PXmlTag(name, attr);
        }

        private KeyValuePair<string, string> ParseXmlAttribute()
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
            return new KeyValuePair<string, string>(key, value);
        }

        private readonly struct PXmlTag
        {
            public PXmlTag(string name, ImmutableDictionary<string, string> attributes)
            {
                Name = name;
                Attributes = attributes;
            }

            public string Name { get; }
            public ImmutableDictionary<string, string> Attributes { get; }
        }
    }
}
