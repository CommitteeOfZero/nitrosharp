using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UtfUnknown;

namespace NitroSharp.NsScript;

public sealed class SourceText
{
    public static readonly Encoding DefaultEncoding;

    static SourceText()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        DefaultEncoding = Encoding.GetEncoding("shift-jis");
    }

    private readonly List<int> _lineStarts;

    private SourceText(string text, ResolvedPath filePath)
    {
        Source = text;
        FilePath = filePath;
        _lineStarts = GetLineStarts();
    }

    public string Source { get; }
    public ResolvedPath FilePath { get; }
    public int Length => Source.Length;

    public int LineCount => _lineStarts.Count;

    public static SourceText From(string text) => new(text, new ResolvedPath(string.Empty));
    public static SourceText From(Stream stream, ResolvedPath filePath, Encoding? encoding = null)
    {
        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must support read operation.", nameof(stream));
        }

        encoding ??= CharsetDetector.DetectFromStream(stream).Detected?.Encoding ?? Encoding.UTF8;
        stream.Seek(0, SeekOrigin.Begin);
        string text = ReadStream(stream, encoding);
        return new SourceText(text, filePath);
    }

    public string GetText(TextSpan textSpan)
        => Source.Substring(textSpan.Start, textSpan.Length);

    public ReadOnlySpan<char> GetCharacterSpan(TextSpan textSpan)
        => Source.AsSpan().Slice(textSpan.Start, textSpan.Length);

    public TextLine GetLine(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= _lineStarts.Count)
        {
            ThrowHelper.ThrowArgumentOutOfRange(nameof(lineIndex));
        }

        int start = _lineStarts[lineIndex];
        int end = lineIndex == _lineStarts.Count - 1 ? Length : _lineStarts[lineIndex + 1];
        return new TextLine(this, TextSpan.FromBounds(start, end));
    }

    public TextLine GetLineFromPosition(int position)
        => GetLine(GetLineNumberFromPosition(position));

    public LinePosition GetLinePosition(int position)
    {
        int lineNumber = GetLineNumberFromPosition(position);
        int column = position - GetLine(lineNumber).Start;
        return new LinePosition(lineNumber, column);
    }

    public LinePositionSpan GetLinePositionSpan(TextSpan textSpan)
        => new(GetLinePosition(textSpan.Start), GetLinePosition(textSpan.End));

    internal int GetLineNumberFromPosition(int position)
    {
        Debug.Assert(position <= Length);
        int lower = 0;
        int upper = _lineStarts.Count - 1;
        while (lower <= upper)
        {
            int index = lower + ((upper - lower) / 2);
            int lineStart = _lineStarts[index];
            if (lineStart == position)
            {
                return index;
            }
            if (lineStart > position)
            {
                upper = index - 1;
            }
            else
            {
                lower = index + 1;
            }
        }

        return lower - 1;
    }

    private List<int> GetLineStarts()
    {
        var lineStarts = new List<int>(Source.Length / 80);
        int position = 0;
        int lineStart = 0;
        while (position < Length)
        {
            int lineBreakWidth = GetLineBreakWidth(Source, position);
            if (lineBreakWidth == 0)
            {
                position++;
            }
            else
            {
                lineStarts.Add(lineStart);
                position += lineBreakWidth;
                lineStart = position;
            }
        }

        if (lineStart <= position)
        {
            lineStarts.Add(lineStart);
        }

        return lineStarts;
    }

    private static int GetLineBreakWidth(string text, int position)
    {
        char c = text[position];
        if (c == '\r')
        {
            if (++position < text.Length && text[position] == '\n')
            {
                return 2;
            }

            return 1;
        }

        return c == '\n' ? 1 : 0;
    }

    private static string ReadStream(Stream stream, Encoding encoding)
    {
        using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true))
        {
            return reader.ReadToEnd();
        }
    }
}

public readonly struct TextLine
{
    private readonly SourceText _sourceText;

    internal TextLine(SourceText sourceText, TextSpan fullSpan)
    {
        _sourceText = sourceText;
        SpanWithLinebreak = fullSpan;
    }

    public TextSpan SpanWithLinebreak { get; }

    public TextSpan Span =>
        new(
            SpanWithLinebreak.Start,
            SpanWithLinebreak.Length - GetLineBreakWidth(_sourceText.GetCharacterSpan(SpanWithLinebreak))
        );

    public int Start => Span.Start;
    public int End => Span.End;

    private static int GetLineBreakWidth(ReadOnlySpan<char> text)
    {
        if (text.Length == 0) { return 0; }

        int pos = text.Length - 1;
        while (pos >= 0 && text[pos] is '\r' or '\n')
        {
            pos--;
        }

        return text.Length - pos - 1;
    }

    public ReadOnlySpan<char> AsSpan() => _sourceText.GetCharacterSpan(Span);
    public override string ToString() => _sourceText.GetText(Span);
}
