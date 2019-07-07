using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace NitroSharp.NsScript.Text
{
    public sealed class SourceText
    {
        private static readonly Encoding s_defaultEncoding;

        private readonly string _source;
        private readonly List<TextSpan> _lines;

        static SourceText()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            s_defaultEncoding = Encoding.GetEncoding("shift-jis");
        }

        private SourceText(string text, ResolvedPath filePath)
        {
            _source = text;
            FilePath = filePath;
            _lines = GetLines();
        }

        public string Source => _source;
        public ResolvedPath FilePath { get; }
        public char this[int index] => Source[index];
        public int Length => _source.Length;

        internal List<TextSpan> Lines => _lines;

        public static SourceText From(string text) => new SourceText(text, new ResolvedPath(string.Empty));
        public static SourceText From(Stream stream, ResolvedPath filePath, Encoding? encoding = null)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must support read operation.", nameof(stream));
            }

            encoding = encoding ?? s_defaultEncoding;
            string text = ReadStream(stream, encoding);
            return new SourceText(text, filePath);
        }

        public TextSpan GetLineSpanFromPosition(int position)
        {
            if (position < 0 || position >= Length)
            {
                ThrowHelper.ThrowOutOfRange(nameof(position));
            }

            int lineNumber = GetLineNumberFromPosition(position);
            return _lines[lineNumber];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetText(TextSpan textSpan) => _source.Substring(textSpan.Start, textSpan.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetCharacterSpan(TextSpan textSpan)
            => _source.AsSpan().Slice(textSpan.Start, textSpan.Length);

        public int GetLineNumberFromPosition(int position)
        {
            // Find the right line using binary search
            int lower = 0;
            int upper = _lines.Count - 1;
            while (lower <= upper)
            {
                int index = lower + ((upper - lower) / 2);
                TextSpan currentLine = _lines[index];
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
            var lines = new List<TextSpan>(_source.Length / 80);
            int position = 0;
            int lineStart = 0;
            while (position < Length)
            {
                int lineBreakWidth = GetLineBreakWidth(_source, position);
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
