using System;
using System.Collections.Immutable;
using System.Diagnostics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Syntax.Markup;
using Veldrid;

namespace NitroSharp.Text
{
    internal sealed class Dialogue
    {
        private static readonly MarkupAstFlattener s_treeFlattener = new();

        private Dialogue(ImmutableArray<DialogueSegment> segments, VoiceSegment? voice)
        {
            Segments = segments;
            Voice = voice;
        }

        public ImmutableArray<DialogueSegment> Segments { get; }
        public VoiceSegment? Voice { get; }

        public static Dialogue Parse(string markup, FontConfiguration fontConfig)
        {
            MarkupContent root = Parsing.ParseMarkup(markup);
            return s_treeFlattener.FlattenContent(root, fontConfig);
        }

        public static TextSegment ParseTextSegment(string text, FontConfiguration fontConfig)
        {
            Dialogue dialogue = Parse(text, fontConfig);
            return dialogue.Segments.Length == 1 && dialogue.Segments[0] is TextSegment ts
                ? ts
                : throw new InvalidOperationException($"Not a valid text segment: '{text}'");
        }

        private sealed class MarkupAstFlattener : MarkupNodeVisitor
        {
            private struct TextRunData
            {
                public string? Text;
                public string? RubyText;
                public int? FontSize;
                public RgbaFloat? Color;
                public RgbaFloat? OutlineColor;
                public bool Italic;
            }

            private FontConfiguration? _fontConfig;
            private readonly ImmutableArray<DialogueSegment>.Builder _segments;
            private readonly ImmutableArray<TextRun>.Builder _textRuns;
            private TextRunData _textRunData;
            private VoiceSegment? _voice;

            public MarkupAstFlattener()
            {
                _segments = ImmutableArray.CreateBuilder<DialogueSegment>(4);
                _textRuns = ImmutableArray.CreateBuilder<TextRun>(1);
            }

            public Dialogue FlattenContent(MarkupNode rootNode, FontConfiguration fontConfig)
            {
                _fontConfig = fontConfig;
                _segments.Clear();
                _voice = null;
                Visit(rootNode);
                FinalizeTextRun();
                FinalizeTextSegment();
                return new Dialogue(_segments.ToImmutable(), _voice);
            }

            public override void VisitContent(MarkupContent content)
            {
                foreach (MarkupNode child in content.Children)
                {
                    Visit(child);
                    FinalizeTextRun();
                }
            }

            public override void VisitFontElement(FontElement fontElement)
            {
                TextRunData oldData = _textRunData;
                _textRunData.FontSize = fontElement.Size;
                if (fontElement.Color.HasValue)
                {
                    NsColor color = fontElement.Color.Value;
                    _textRunData.Color = color.ToRgbaFloat();
                }
                if (fontElement.OutlineColor.HasValue)
                {
                    NsColor outlineColor = fontElement.OutlineColor.Value;
                    _textRunData.OutlineColor = outlineColor.ToRgbaFloat();
                }

                Visit(fontElement.Content);
                _textRunData = oldData;
            }

            public override void VisitSpanElement(SpanElement spanElement)
            {
                TextRunData oldData = _textRunData;
                _textRunData.FontSize = spanElement.Size;
                Visit(spanElement.Content);
                _textRunData = oldData;
            }

            public override void VisitText(MarkupText text)
            {
                if (text.Text.Length > 0)
                {
                    _textRunData.Text = text.Text;
                }
            }

            public override void VisitLinebreakElement(LinebreakElement linebreakElement)
            {
                _textRunData.Text = "\n";
            }

            public override void VisitItalicElement(ItalicElement italicElement)
            {
                bool oldValue = _textRunData.Italic;
                _textRunData.Italic = true;
                VisitContent(italicElement.Content);
                _textRunData.Italic = oldValue;
            }

            public override void VisitRubyElement(RubyElement rubyElement)
            {
                TextRunData oldData = _textRunData;
                _textRunData.RubyText = rubyElement.RubyText;
                Visit(rubyElement.RubyBase);
                _textRunData = oldData;
            }

            public override void VisitHaltElement(HaltElement haltElement)
            {
                FinalizeTextSegment();
                _segments.Add(new MarkerSegment(MarkerKind.Halt));
            }

            public override void VisitNoLinebreaksElement(NoLinebreaksElement element)
            {
                FinalizeTextSegment();
                _segments.Add(new MarkerSegment(MarkerKind.NoLinebreaks));
            }

            public override void VisitVoiceElement(VoiceElement node)
            {
                FinalizeTextSegment();
                _voice = new VoiceSegment(node.CharacterName, node.FileName, node.Action);
                _segments.Add(_voice);
            }

            private void FinalizeTextRun()
            {
                Debug.Assert(_fontConfig is not null);
                ref readonly TextRunData data = ref _textRunData;
                if (data.Text is null) { return; }
                FontFaceKey font = _fontConfig.DefaultFont;
                if (data.Italic && _fontConfig.ItalicFont.HasValue)
                {
                    font = _fontConfig.ItalicFont.Value;
                }

                PtFontSize fontSize = data.FontSize.HasValue
                    ? new PtFontSize(data.FontSize.Value)
                    : _fontConfig.DefaultFontSize;

                RgbaFloat color = data.Color ?? new RgbaFloat(_fontConfig.DefaultTextColor);
                RgbaFloat? outlineColor = data.OutlineColor ??
                    _fontConfig.DefaultOutlineColor?.ToRgbaFloat();

                TextRun textRun;
                if (data.RubyText is null)
                {
                    textRun = TextRun.Regular(
                        data.Text.AsMemory(),
                        font, fontSize,
                        color, outlineColor
                    );
                }
                else
                {
                    textRun = TextRun.WithRubyText(
                        data.Text.AsMemory(),
                        data.RubyText.AsMemory(),
                        font, fontSize,
                        color, outlineColor
                    );
                }
                _textRuns.Add(textRun);
                _textRunData.RubyText = null;
                _textRunData.Text = null;
            }

            private void FinalizeTextSegment()
            {
                if (_textRuns.Count > 0)
                {
                    _segments.Add(new TextSegment(_textRuns.ToImmutable()));
                    _textRuns.Clear();
                }
            }
        }
    }

    internal enum DialogueSegmentKind
    {
        Text,
        Voice,
        Marker
    }

    internal abstract class DialogueSegment
    {
        public abstract DialogueSegmentKind SegmentKind { get; }
    }

    internal sealed class TextSegment : DialogueSegment
    {
        public TextSegment(ImmutableArray<TextRun> textRuns)
        {
            TextRuns = textRuns;
        }

        public ImmutableArray<TextRun> TextRuns { get; }
        public override DialogueSegmentKind SegmentKind => DialogueSegmentKind.Text;
    }

    internal sealed class VoiceSegment : DialogueSegment
    {
        public VoiceSegment(string characterName, string fileName, NsVoiceAction action)
        {
            CharacterName = characterName;
            FileName = fileName;
            Action = action;
        }

        public string CharacterName { get; }
        public string FileName { get; }
        public NsVoiceAction Action { get; }

        public override DialogueSegmentKind SegmentKind => DialogueSegmentKind.Voice;
    }

    internal enum MarkerKind
    {
        Halt,
        NoLinebreaks
    }

    internal sealed class MarkerSegment : DialogueSegment
    {
        public MarkerSegment(MarkerKind kind)
        {
            MarkerKind = kind;
        }

        public MarkerKind MarkerKind { get; }
        public override DialogueSegmentKind SegmentKind => DialogueSegmentKind.Marker;
    }
}
