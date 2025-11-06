using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

    private readonly List<TextSpan> _lineSpans;

    private SourceText(string text, ResolvedPath filePath)
    {
        Source = text;
        FilePath = filePath;
        _lineSpans = GetLines();
    }

    public string Source { get; }
    public ResolvedPath FilePath { get; }
    public int Length => Source.Length;

    public ReadOnlySpan<TextSpan> LineSpans => CollectionsMarshal.AsSpan(_lineSpans);
    public int LineCount => _lineSpans.Count;

    public static SourceText From(string text) => new(text, new ResolvedPath(string.Empty));
    public static SourceText From(Stream stream, ResolvedPath filePath, Encoding? encoding = null)
    {
        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must support read operation.", nameof(stream));
        }

        encoding ??= CharsetDetector.DetectFromStream(stream).Detected.Encoding;
        stream.Seek(0, SeekOrigin.Begin);
        string text = ReadStream(stream, encoding);
        return new SourceText(text, filePath);
    }

    public string GetText(TextSpan textSpan)
        => Source.Substring(textSpan.Start, textSpan.Length);

    public ReadOnlySpan<char> GetCharacterSpan(TextSpan textSpan)
        => Source.AsSpan().Slice(textSpan.Start, textSpan.Length);

    public string GetLineText(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= _lineSpans.Count)
        {
            ThrowHelper.ThrowArgumentOutOfRange(nameof(lineIndex));
        }
        TextSpan lineSpan = LineSpans[lineIndex];
        return Source.Substring(lineSpan.Start, lineSpan.Length);
    }

    public LinePosition GetLinePosition(int position)
    {
        int line = GetLineNumberFromPosition(position);
        TextSpan lineSpan = LineSpans[line];
        int column = position - lineSpan.Start;
        return new LinePosition(line, column);
    }

    public LinePositionSpan GetLinePositionSpan(TextSpan textSpan)
        => new(GetLinePosition(textSpan.Start), GetLinePosition(textSpan.End));

    internal int GetLineNumberFromPosition(int position)
    {
        Debug.Assert(position < Length);
        int lower = 0;
        int upper = _lineSpans.Count - 1;
        while (lower <= upper)
        {
            int index = lower + ((upper - lower) / 2);
            int start = _lineSpans[index].Start;
            if (start == position)
            {
                return index;
            }
            if (start > position)
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

    private List<TextSpan> GetLines()
    {
        var lines = new List<TextSpan>(Source.Length / 80);
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
                lines.Add(new TextSpan(lineStart, position - lineStart));
                position += lineBreakWidth;
                lineStart = position;
            }
        }

        if (lineStart <= position)
        {
            lines.Add(new TextSpan(lineStart, Length - lineStart));
        }

        return lines;
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
