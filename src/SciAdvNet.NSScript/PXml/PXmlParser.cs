using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace SciAdvNet.NSScript.PXml
{
    public class PXmlParser
    {
        private TextScanner _scanner;

        public PXmlParser(string sourceText)
        {
            _scanner = new TextScanner(sourceText);
        }


        private PXmlTag ParseXmlTag()
        {
            while (_scanner.PeekChar() == '<' || _scanner.PeekChar() == '/')
            {
                _scanner.EatChar(_scanner.PeekChar());
            }

            _scanner.StartScanning();
            char c;
            while (!SyntaxFacts.IsWhitespace(c = _scanner.PeekChar()) && !SyntaxFacts.IsNewLine(c) && c != '>')
            {
                _scanner.AdvanceChar();
            }

            string name = _scanner.CurrentLexeme;
            var attributes = ImmutableDictionary.CreateBuilder<string, string>();
            while ((c = _scanner.PeekChar()) != '>')
            {
                if (c == ' ' || SyntaxFacts.IsNewLine(c))
                {
                    _scanner.AdvanceChar();
                }
                else
                {
                    attributes.Add(ParseXmlAttribute());
                }
            }

            return new PXmlTag(name, attributes.ToImmutable());
        }

        private KeyValuePair<string, string> ParseXmlAttribute()
        {
            char c;
            _scanner.StartScanning();
            while ((c = _scanner.PeekChar()) != '=')
            {
                _scanner.AdvanceChar();
            }
            string key = _scanner.CurrentLexeme;

            _scanner.EatChar('=');
            _scanner.EatChar('"');

            _scanner.StartScanning();
            while ((c = _scanner.PeekChar()) != '"')
            {
                _scanner.AdvanceChar();
            }
            string value = _scanner.CurrentLexeme;
            _scanner.EatChar('"');

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
