using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UtfUnknown;

namespace NitroSharp.NsScript
{
    public sealed class SourceText
    {
        public static readonly Encoding DefaultEncoding;

        static SourceText()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            DefaultEncoding = Encoding.GetEncoding("shift-jis");
        }

        private SourceText(string text, ResolvedPath filePath)
        {
            Source = text;
            FilePath = filePath;
            Lines = GetLines();
        }

        public string Source { get; }
        public ResolvedPath FilePath { get; }
        public int Length => Source.Length;

        internal List<TextSpan> Lines { get; }

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

        public TextSpan GetLineSpanFromPosition(int position)
        {
            if (position < 0 || position > Length)
            {
                ThrowHelper.ThrowOutOfRange(nameof(position));
            }

            int lineNumber = GetLineNumberFromPosition(position);
            return Lines[lineNumber];
        }

        public string GetText(TextSpan textSpan) => Source.Substring(textSpan.Start, textSpan.Length);

        public ReadOnlySpan<char> GetCharacterSpan(TextSpan textSpan)
            => Source.AsSpan().Slice(textSpan.Start, textSpan.Length);

        public int GetLineNumberFromPosition(int position)
        {
            // Find the right line using binary search
            int lower = 0;
            int upper = Lines.Count - 1;
            while (lower <= upper)
            {
                int index = lower + ((upper - lower) / 2);
                TextSpan currentLine = Lines[index];
                int start = currentLine.Start;
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
}
