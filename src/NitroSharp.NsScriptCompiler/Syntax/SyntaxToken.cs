using NitroSharp.NsScriptNew.Text;
using System;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScriptNew.Syntax
{
    public readonly struct SyntaxToken
    {
        public SyntaxToken(SyntaxTokenKind kind, TextSpan textSpan, SyntaxTokenFlags flags)
        {
            TextSpan = textSpan;
            Kind = kind;
            Flags = flags;
        }

        public readonly TextSpan TextSpan;
        public readonly SyntaxTokenKind Kind;
        public readonly SyntaxTokenFlags Flags;

        public bool IsFloatingPointLiteral =>
            (Flags & SyntaxTokenFlags.HasDecimalPoint) == SyntaxTokenFlags.HasDecimalPoint;

        public bool IsHexTriplet =>
            (Flags & SyntaxTokenFlags.IsHexTriplet) == SyntaxTokenFlags.IsHexTriplet;

        public bool HasSigil =>
            (Flags & SyntaxTokenFlags.HasDollarPrefix) == SyntaxTokenFlags.HasDollarPrefix ||
            (Flags & SyntaxTokenFlags.HasHashPrefix) == SyntaxTokenFlags.HasHashPrefix ||
            (Flags & SyntaxTokenFlags.HasAtPrefix) == SyntaxTokenFlags.HasAtPrefix;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetText(SourceText sourceText)
        {
            return sourceText.GetCharacterSpan(TextSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetValueText(SourceText sourceText)
        {
            return sourceText.GetCharacterSpan(GetValueSpan());
        }

        public TextSpan GetValueSpan()
        {
            int start = TextSpan.Start;
            int end = TextSpan.End;
            if ((Flags & SyntaxTokenFlags.IsQuoted) == SyntaxTokenFlags.IsQuoted)
            {
                start++;
                end--;

            }
            if ((Flags & SyntaxTokenFlags.HasDollarPrefix) == SyntaxTokenFlags.HasDollarPrefix
                || (Flags & SyntaxTokenFlags.HasHashPrefix) == SyntaxTokenFlags.HasHashPrefix
                || (Flags & SyntaxTokenFlags.HasAtPrefix) == SyntaxTokenFlags.HasAtPrefix
                || (Flags & SyntaxTokenFlags.IsHexTriplet) == SyntaxTokenFlags.IsHexTriplet)
            {
                start++;
            }

            return TextSpan.FromBounds(start, end);
        }

        public override string ToString() => $"{TextSpan.ToString()} <{Kind}>";
    }

    [Flags]
    public enum SyntaxTokenFlags : byte
    {
        Empty = 0,
        HasDiagnostics = 1 << 0,
        IsQuoted = 1 << 1,
        HasDollarPrefix = 1 << 2,
        HasHashPrefix = 1 << 3,
        HasAtPrefix = 1 << 4,
        HasDecimalPoint = 1 << 5,
        IsHexTriplet = 1 << 6
    }
}
