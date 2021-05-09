using System;
using System.Collections.Generic;
using System.Text;
using static NitroSharp.Text.LineBreakTables;

namespace NitroSharp.Text
{
    public enum LineBreakKind
    {
        Hard,
        Soft
    }

    public readonly struct LineBreak : IEquatable<LineBreak>
    {
        public readonly int PosInScalars;
        public readonly LineBreakKind Kind;

        public LineBreak(int posInScalars, LineBreakKind kind)
        {
            PosInScalars = posInScalars;
            Kind = kind;
        }

        public bool Equals(LineBreak other)
            => PosInScalars == other.PosInScalars && Kind == other.Kind;

        public override int GetHashCode()
            => HashCode.Combine(PosInScalars, Kind);

        public override string ToString() => $"<{PosInScalars}, {Kind}>";
    }

    internal readonly ref struct LineBreaker
    {
        private readonly ReadOnlySpan<char> _text;

        public LineBreaker(ReadOnlySpan<char> text) => _text = text;
        public LineBreakEnumerator GetEnumerator() => new(_text);

        public LineBreak[] ToArray()
        {
            var list = new List<LineBreak>();
            foreach (LineBreak lb in this)
            {
                list.Add(lb);
            }
            return list.ToArray();
        }
    }

    // Breaks lines in accordance with the Unicode line breaking algorithm
    // Ported from https://github.com/alexheretic/glyph-brush
    internal ref struct LineBreakEnumerator
    {
        private SpanRuneEnumerator _scalars;
        private int _posInScalars;
        private int _lenInScalars;
        private byte _state;

        public LineBreakEnumerator(ReadOnlySpan<char> text)
        {
            _scalars = text.EnumerateRunes();
            _state = 0;
            _lenInScalars = 0;
            _posInScalars = !text.IsEmpty ? 0 : 1;
            Current = default;
        }

        public LineBreak Current { get; private set; }

        public bool MoveNext()
        {
            while (true)
            {
                if (_scalars.MoveNext())
                {
                    Rune scalar = _scalars.Current;
                    byte lbProperty = GetLineBreakProperty(scalar.Value);
                    byte newState;
                    if (_posInScalars == 0)
                    {
                        _state = newState = lbProperty;
                    }
                    else
                    {
                        int i = _state * LinebreakCategoryCount + lbProperty;
                        newState = LinebreakStateMachine[i];
                    }
                    int pos = _posInScalars++;
                    _lenInScalars++;
                    if ((sbyte)newState < 0)
                    {
                        _state = (byte)(newState & 0x3F);
                        SetCurrent(pos, newState);
                        return true;
                    }
                    _state = newState;
                }
                else
                {
                    if (_posInScalars == _lenInScalars)
                    {
                        _posInScalars++;
                        int i = _state * LinebreakCategoryCount;
                        byte state = LinebreakStateMachine[i];
                        SetCurrent(_lenInScalars, state);
                        return true;
                    }

                    return false;
                }
            }
        }

        private void SetCurrent(int pos, byte state)
        {
            LineBreakKind kind = state >= 0xC0
                ? LineBreakKind.Hard
                : LineBreakKind.Soft;
            Current = new LineBreak(pos, kind);
        }

        private static byte GetLineBreakProperty(int codepoint)
        {
            switch (codepoint)
            {
                case < 0x800:
                    return Linebreak12[codepoint];
                case < 0x10000:
                    byte child = Linebreak3Root[codepoint >> 6];
                    return Linebreak3Child[child * 0x40 + (codepoint & 0x3F)];
                default:
                    byte mid = Linebreak4Root[codepoint >> 12];
                    byte leaf = Linebreak4Mid[mid * 0x40 + ((codepoint >> 6) & 0x3F)];
                    return Linebreak4Leaves[leaf * 0x40 + (codepoint & 0x3F)];
            }
        }
    }
}
