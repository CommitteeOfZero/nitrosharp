using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace SciAdvNet.NSScript.PXml
{
    public class PXmlParser : TextScanner
    {
        public PXmlParser(string sourceText) : base(sourceText)
        {
        }

        public PXmlContent Parse()
        {
            return ParseContent(string.Empty);
        }

        private PXmlNode ParseNode()
        {
            SkipTrivia();
            StartScanning();

            char peek = PeekChar();
            if (peek == '<')
            {
                return ParseElement();
            }

            return ParsePlainText();
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
                children.Add(node);
            }

            return new PXmlContent(children.ToImmutable());
        }

        private PXmlText ParsePlainText()
        {
            char c;
            while ((c = PeekChar()) != '<' && c != EofCharacter)
            {
                AdvanceChar();
            }

            var sb = new StringBuilder(CurrentLexeme);
            sb.Replace("&.", ".");
            sb.Replace("&,", ",");

            string processedText = sb.ToString();
            return new PXmlText(processedText);
        }

        private bool IsEndTag() => PeekChar() == '<' && PeekChar(1) == '/';

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

                case "voice":
                default:
                    node = ParseVoiceElement(startTag);
                    break;
            }

            return node;
        }

        private VoiceElement ParseVoiceElement(PXmlTag tag)
        {
            string characterName = tag.Attributes["name"];
            string fileName = tag.Attributes["src"];

            tag.Attributes.TryGetValue("mode", out string mode);
            bool stop = mode == "off";
            var action = stop ? VoiceAction.Stop : VoiceAction.Play;

            return new VoiceElement(action, characterName, fileName);
        }

        private FontColorElement ParseFontElement(PXmlTag startTag)
        {
            string colorString = startTag.Attributes["incolor"];
            NssColor color = PredefinedConstants.ParseColor(colorString);

            var content = ParseContent(startTag.Name);
            return new FontColorElement(color, content);
        }

        private RubyElement ParseRubyElement(PXmlTag startTag)
        {
            string rubyText = startTag.Attributes["text"];
            var rubyBase = ParseContent("RUBY");

            return new RubyElement(rubyBase, rubyText);
        }

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

            string name = CurrentLexeme;
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
            string key = CurrentLexeme;

            EatChar('=');
            EatChar('"');

            StartScanning();
            while ((c = PeekChar()) != '"')
            {
                AdvanceChar();
            }
            string value = CurrentLexeme;
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
