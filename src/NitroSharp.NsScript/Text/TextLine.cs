using System;
using System.Collections.Generic;

namespace NitroSharp.NsScript.Text
{
    public struct TextLine : IEquatable<TextLine>
    {
        private readonly int _start;
        private readonly int _length;

        public TextLine(SourceText text, int start, int length)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            Text = text;
            _start = start;
            _length = length;
        }

        public SourceText Text { get; }
        public TextSpan Span => new TextSpan(_start, _length);

        public string GetText()
        {
            return Text.GetText(_start, _length);
        }

        public override bool Equals(object obj) => obj is TextLine line && Equals(line);
        public bool Equals(TextLine other)
        {
            return _start == other._start &&
                   _length == other._length &&
                   Text == other.Text;
        }

        public override int GetHashCode()
        {
            int hashCode = 764910870;
            hashCode = hashCode * -1521134295 + _start.GetHashCode();
            hashCode = hashCode * -1521134295 + _length.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<SourceText>.Default.GetHashCode(Text);
            return hashCode;
        }

        public static bool operator ==(TextLine left, TextLine right) => left.Equals(right);
        public static bool operator !=(TextLine left, TextLine right) => !left.Equals(right);
    }
}
