//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Diagnostics;
//using System.Globalization;
//using System.IO;

//namespace SciAdvNet.NSScript
//{
//    internal sealed partial class NSScriptParser
//    {
//        private sealed class Scanner
//        {
//            private const char EofCharacter = char.MaxValue;
//            private int _textWindowStart;

//            public Scanner(string text)
//            {
//                Text = text;
//            }

//            public string Text { get; }

//            public bool ReachedEnd => Position >= Text.Length;
//            public int Position { get; private set; }
//            public string TextWindow => Text.Substring(_textWindowStart, Position - _textWindowStart);

//            public void StartScanning() => _textWindowStart = Position;
//            public char PeekChar()
//            {
//                if (ReachedEnd)
//                {
//                    return EofCharacter;
//                }

//                return Text[Position];
//            }

//            public void Advance() => Position++;
//            public void EatChar(char c)
//            {
//                char actualCharacter = PeekChar();
//                if (actualCharacter != c)
//                {
//                    throw new InvalidDataException();
//                }

//                Advance();
//            }
//        }

//        public DialogueBlock ParseDialogueBlock()
//        {
//            Debug.Assert(IsDialogueBlockStart());

//            string boxName = ExtractBoxName();
//            EatToken(SyntaxTokenKind.XmlElementStartTag);
//            var textStartTag = EatToken(SyntaxTokenKind.Xml_TextStartTag);
//            string blockIdentifier = textStartTag.Text.Substring(1, textStartTag.Text.Length - 2);

//            var statements = ImmutableArray.CreateBuilder<Statement>();
//            while (!IsDialogueBlockEnd())
//            {
//                if (CurrentToken.Kind == SyntaxTokenKind.Xml_LineBreak)
//                {
//                    EatToken();
//                    continue;
//                }

//                statements.Add(ParsePart());
//            }

//            EatToken(SyntaxTokenKind.XmlElementEndTag);
//            return StatementFactory.DialogueBlock(blockIdentifier, boxName, statements.ToImmutable());
//        }

//        private bool IsDialogueBlockEnd()
//        {
//            return CurrentToken.Text.Equals("</PRE>", StringComparison.OrdinalIgnoreCase);
//        }

//        private bool IsVoiceTag()
//        {
//            return CurrentToken.Text.ToUpperInvariant().StartsWith("<VOICE");
//        }

//        private Statement ParsePart()
//        {
//            switch (CurrentToken.Kind)
//            {
//                case SyntaxTokenKind.Xml_Text:
//                case SyntaxTokenKind.Xml_VerbatimText:
//                    return ParseDialogueLine();

//                case SyntaxTokenKind.XmlElementStartTag:
//                    if (IsVoiceTag())
//                    {
//                        return ParseVoiceElement();
//                    }
//                    else
//                    {
//                        return ParseDialogueLine();
//                    }

//                case SyntaxTokenKind.OpenBraceToken:
//                    return ParseBlock();

//                default:
//                    throw ExceptionUtilities.UnexpectedToken(FileName, CurrentToken.Text);
//            }
//        }

//        private DialogueLine ParseDialogueLine()
//        {
//            var segments = ImmutableArray.CreateBuilder<PXmlNode>();
//            while (!IsDialogueBlockEnd() && CurrentToken.Kind != SyntaxTokenKind.Xml_LineBreak && CurrentToken.Kind != SyntaxTokenKind.OpenBraceToken)
//            {
//                segments.AddRange(ParseContent(string.Empty));
//            }

//            var content = new PXmlContent(segments.ToImmutable());
//            if (CurrentToken.Kind == SyntaxTokenKind.Xml_LineBreak)
//            {
//                EatToken();
//            }

//            return StatementFactory.DialogueLine(content);
//        }

//        private ImmutableArray<PXmlNode> ParseContent(string elementName)
//        {
//            var nodes = ImmutableArray.CreateBuilder<PXmlNode>();
//            while (!IsDialogueBlockEnd() && CurrentToken.Kind != SyntaxTokenKind.Xml_LineBreak && CurrentToken.Kind != SyntaxTokenKind.OpenBraceToken)
//            {
//                switch (CurrentToken.Kind)
//                {
//                    case SyntaxTokenKind.XmlElementEndTag:
//                        var endTag = ParseXmlTag();
//                        if (endTag.Name.Equals(elementName, StringComparison.OrdinalIgnoreCase))
//                        {
//                            return nodes.ToImmutable();
//                        }
//                        break;

//                    case SyntaxTokenKind.Xml_Text:
//                        string s = EatToken().Text;
//                        nodes.Add(StatementFactory.Text(s));
//                        break;

//                    case SyntaxTokenKind.Xml_VerbatimText:
//                        s = EatToken().Text;
//                        nodes.Add(StatementFactory.VerbatimText(s));
//                        break;

//                    case SyntaxTokenKind.XmlElementStartTag:
//                        var startTag = ParseXmlTag();
//                        string tagName = startTag.Name.ToUpperInvariant();
//                        //if (tagName == "K" || tagName == "?")
//                        //{
//                        //    break;
//                        //}

//                        switch (tagName)
//                        {
//                            case "FONT":
//                                string strColor = startTag.Attributes["incolor"].Substring(1);
//                                int colorCode = int.Parse(strColor, NumberStyles.HexNumber);

//                                var fontElementContent = new PXmlContent(ParseContent("FONT"));
//                                nodes.Add(StatementFactory.ColorElement(colorCode, fontElementContent));
//                                break;

//                            case "RUBY":
//                                string rubyText = startTag.Attributes["text"];
//                                var rubyBase = new PXmlContent(ParseContent("RUBY"));
//                                nodes.Add(StatementFactory.RubyElement(rubyBase, rubyText));
//                                break;
//                        }
//                        break;
//                }
//            }

//            return nodes.ToImmutable();
//        }

//        private string ExtractBoxName()
//        {
//            var tag = CurrentToken;
//            int idxStart = 5;
//            int idxEnd = tag.Text.Length - 1;

//            return tag.Text.Substring(idxStart, idxEnd - idxStart);
//        }

//        private Voice ParseVoiceElement()
//        {
//            var tag = ParseXmlTag();
//            string fileName = tag.Attributes["src"];
//            string character = tag.Attributes["name"];
//            return StatementFactory.Voice(character, fileName);
//        }

//        private sealed class PseudoXmlTag
//        {
//            public PseudoXmlTag(string name, ImmutableDictionary<string, string> attributes)
//            {
//                Name = name;
//                Attributes = attributes;
//            }

//            public string Name { get; }
//            public ImmutableDictionary<string, string> Attributes { get; }
//        }

//        private PseudoXmlTag ParseXmlTag()
//        {
//            string text = CurrentToken.Text;
//            var scanner = new Scanner(text);
//            while (scanner.PeekChar() == '<' || scanner.PeekChar() == '/')
//            {
//                scanner.EatChar(scanner.PeekChar());
//            }

//            scanner.StartScanning();
//            char c;
//            while (!SyntaxFacts.IsWhitespace(c = scanner.PeekChar()) && !SyntaxFacts.IsNewLine(c) && c != '>')
//            {
//                scanner.Advance();
//            }

//            string name = scanner.TextWindow;
//            var attributes = ImmutableDictionary.CreateBuilder<string, string>();
//            while ((c = scanner.PeekChar()) != '>')
//            {
//                if (c == ' ' || SyntaxFacts.IsNewLine(c))
//                {
//                    scanner.Advance();
//                }
//                else
//                {
//                    attributes.Add(ParseXmlAttribute(scanner));
//                }
//            }

//            EatToken();
//            return new PseudoXmlTag(name, attributes.ToImmutable());
//        }

//        private KeyValuePair<string, string> ParseXmlAttribute(Scanner scanner)
//        {
//            char c;
//            scanner.StartScanning();
//            while ((c = scanner.PeekChar()) != '=')
//            {
//                scanner.Advance();
//            }
//            string key = scanner.TextWindow;

//            scanner.EatChar('=');
//            scanner.EatChar('"');

//            scanner.StartScanning();
//            while ((c = scanner.PeekChar()) != '"')
//            {
//                scanner.Advance();
//            }
//            string value = scanner.TextWindow;
//            scanner.EatChar('"');

//            return new KeyValuePair<string, string>(key, value);
//        }
//    }
//}
