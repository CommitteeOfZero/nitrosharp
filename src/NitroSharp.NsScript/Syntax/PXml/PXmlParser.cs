using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace NitroSharp.NsScript.Syntax.PXml
{
    internal sealed class PXmlParser : TextScanner
    {
        public PXmlParser(string sourceText) : base(sourceText)
        {
        }

        public PXmlContent Parse()
        {
            return ParseContent(string.Empty);
        }

        private PXmlContent ParseContent(string rootElementName)
        {
            var children = ImmutableArray.CreateBuilder<PXmlNode>();
            while (PeekChar() != EofCharacter)
            {
                if (IsEndTag())
                {
                    var endTag = ParsePXmlTag();
                    if (rootElementName == endTag.Name)
                    {
                        break;
                    }
                }

                var node = ParseNode();
                if (node != null)
                {
                    children.Add(node);
                }
            }

            return new PXmlContent(children.ToImmutable());
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
            var startTag = ParsePXmlTag();
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
                    node = ParsePlainText();
                    ParsePXmlTag();
                    break;

                case "voice":
                    node = ParseVoiceElement(startTag);
                    break;

                // TODO: actually figure out what <k> and <?> do.
                case "k":
                case "?":
                    return null;

                default:
                    throw new NotImplementedException($"PXml tag '{startTag.Name}' is not yet supported.");
            }

            return node;
        }

        private VoiceElement ParseVoiceElement(PXmlTag tag)
        {
            string characterName = tag.Attributes["name"];
            string fileName = tag.Attributes["src"];

            tag.Attributes.TryGetValue("mode", out string mode);
            bool stop = mode == "off";
            var action = stop ? NsVoiceAction.Stop : NsVoiceAction.Play;

            return new VoiceElement(action, characterName, fileName);
        }

        private ColorElement ParseFontElement(PXmlTag startTag)
        {
            string colorString = startTag.Attributes["incolor"];
            NsColor color = NsColor.FromString(colorString);

            var content = ParseContent(startTag.Name);
            return new ColorElement(color, content);
        }

        private RubyElement ParseRubyElement(PXmlTag startTag)
        {
            string rubyText = startTag.Attributes["text"];
            var rubyBase = ParseContent("RUBY");

            return new RubyElement(rubyBase, rubyText);
        }

        private PXmlText ParsePlainText()
        {
            StartScanning();
            var sb = new StringBuilder();

            char c;
            while ((c = PeekChar()) != '<' && c != EofCharacter)
            {
                if (c == '/' && PeekChar() == '/')
                {
                    sb.Append(GetCurrentLexeme());
                    ScanToEndOfLine();
                    StartScanning();
                }

                AdvanceChar();
            }

            sb.Append(GetCurrentLexeme());
            sb.Replace("&.", ".");
            sb.Replace("&,", ",");

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
            while (!SyntaxFacts.IsWhitespace(c = PeekChar()) && !SyntaxFacts.IsNewLine(c) && c != '>')
            {
                AdvanceChar();
            }

            string name = GetCurrentLexeme();
            var attributes = ImmutableDictionary.CreateBuilder<string, string>();
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
            return new PXmlTag(name, attributes.ToImmutable());
        }

        private KeyValuePair<string, string> ParseXmlAttribute()
        {
            char c;
            StartScanning();
            while ((c = PeekChar()) != '=')
            {
                AdvanceChar();
            }
            string key = GetCurrentLexeme();

            EatChar('=');
            EatChar('"');

            StartScanning();
            while ((c = PeekChar()) != '"')
            {
                AdvanceChar();
            }
            string value = GetCurrentLexeme();
            EatChar('"');

            return new KeyValuePair<string, string>(key, value);
        }

        private sealed class PXmlTag
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
