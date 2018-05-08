using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace NitroSharp.NsScript.Text
{
    public sealed class SourceText
    {
        private static readonly Encoding s_defaultEncoding;

        static SourceText()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            s_defaultEncoding = Encoding.GetEncoding("shift-jis");
        }

        private SourceText(string text, string filePath)
        {
            Source = text ?? throw new ArgumentNullException(nameof(text));
            FilePath = filePath;
            Lines = GetLines();
        }

        public string Source { get; }
        public string FilePath { get; }
        public char this[int index] => Source[index];
        public int Length => Source.Length;
        public ImmutableArray<TextLine> Lines { get; }

        public static SourceText From(string text) => new SourceText(text, string.Empty);
        public static SourceText From(Stream stream, string filePath, Encoding encoding = null)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must support read operation.", nameof(stream));
            }

            encoding = encoding ?? s_defaultEncoding;
            string text = ReadStream(stream, encoding);
            return new SourceText(text, filePath);
        }

        public TextLine GetLineFromPosition(int position)
        {
            if (position < 0 || position >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            int lineNumber = GetLineNumberFromPosition(position);
            return Lines[lineNumber];
        }

        public string GetText(TextSpan textSpan) => Source.Substring(textSpan.Start, textSpan.Length);
        public string GetText(int position, int length) => GetText(new TextSpan(position, length));

        public int GetLineNumberFromPosition(int position)
        {
            // Perform binary search to find the right line.
            int lower = 0;
            int upper = Lines.Length - 1;
            while (lower <= upper)
            {
                int index = lower + ((upper - lower) >> 1);
                var currentLine = Lines[index];
                int start = currentLine.Span.Start;
                if (start == position)
                {
                    return index;
                }
                else if (start > position)
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

        private ImmutableArray<TextLine> GetLines()
        {
            var lines = ImmutableArray.CreateBuilder<TextLine>();
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
                    lines.Add(new TextLine(this, lineStart, position - lineStart));
                    position += lineBreakWidth;
                    lineStart = position;
                }
            }

            if (lineStart <= position)
            {
                lines.Add(new TextLine(this, lineStart, Length - lineStart));
            }

            return lines.ToImmutable();
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
}
