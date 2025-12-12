using System;
using System.Buffers;

namespace NitroSharp.NsScript;

public readonly record struct EntityPattern(string Value, bool ContainsWildcard)
{
    public bool Match(string s)
    {
        string patternText = Value;
        int rowLength = patternText.Length + 1;
        bool[] prevRow = ArrayPool<bool>.Shared.Rent(rowLength);
        bool[] currRow = ArrayPool<bool>.Shared.Rent(rowLength);

        prevRow.AsSpan(..rowLength).Clear();
        prevRow[0] = true;
        for (int j = 1; j <= patternText.Length; j++)
        {
            if (patternText[j - 1] == '*')
            {
                prevRow[j] = prevRow[j - 1];
            }
        }

        for (int i = 1; i <= s.Length; i++)
        {
            currRow.AsSpan(..rowLength).Clear();
            for (int j = 1; j <= patternText.Length; j++)
            {
                char c = patternText[j - 1];
                bool match = false;
                if (c == '*')
                {
                    match = prevRow[j] || currRow[j - 1];
                }
                else if (c == s[i - 1])
                {
                    match = prevRow[j - 1];
                }

                currRow[j] = match;
            }

            (prevRow, currRow) = (currRow, prevRow);
        }

        bool result = prevRow[patternText.Length];
        ArrayPool<bool>.Shared.Return(prevRow);
        ArrayPool<bool>.Shared.Return(currRow);
        return result;
    }

    public static implicit operator EntityPattern(string value)
        => new(value, value.Contains('*'));

    public override string ToString() => Value;
}
