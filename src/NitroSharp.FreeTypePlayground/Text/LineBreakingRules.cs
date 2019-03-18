namespace NitroSharp.Text
{
    internal static class LineBreakingRules
    {
        public static bool CanStartLine(char c)
        {
            return IsJpCharacter(c)
                ? JpCanStartLine(c)
                : char.IsLetterOrDigit(c);
        }

        public static bool CanEndLine(char c)
        {
            return IsJpCharacter(c)
                ? JpCanEndLine(c)
                : char.IsWhiteSpace(c);
        }

        private static bool IsJpCharacter(char c)
        {
            return c >= 0x3000 && c <= 0x303f  // JP punctuation
                || c >= 0x3040 && c <= 0x309f  // Hiragana
                || c >= 0x30a0 && c <= 0x30ff  // Katakana
                || c >= 0x4E00 && c <= 0x9FFF  // Kanji
                || c >= 0xFF5B && c <= 0xFFEF; // Half-width katanana
        }

        private static bool JpCanStartLine(char c)
        {
            switch (c)
            {
                // Closing brackets
                case ')':
                case ']':
                case '｝':
                case '〕':
                case '〉':
                case '》':
                case '」':
                case '』':
                case '】':
                case '〙':
                case '〗':
                case '〟':
                case '’':
                case '"':
                case '｠':
                case '»':

                // Small kana
                case 'ヽ':
                case 'ヾ':
                case 'ー':
                case 'ァ':
                case 'ィ':
                case 'ゥ':
                case 'ェ':
                case 'ォ':
                case 'ッ':
                case 'ャ':
                case 'ュ':
                case 'ョ':
                case 'ヮ':
                case 'ヵ':
                case 'ヶ':
                case 'ぁ':
                case 'ぃ':
                case 'ぅ':
                case 'ぇ':
                case 'ぉ':
                case 'っ':
                case 'ゃ':
                case 'ゅ':
                case 'ょ':
                case 'ゎ':
                case 'ゕ':
                case 'ゖ':
                case 'ㇰ':
                case 'ㇱ':
                case 'ㇲ':
                case 'ㇳ':
                case 'ㇴ':
                case 'ㇵ':
                case 'ㇶ':
                case 'ㇷ':
                case 'ㇸ':
                case 'ㇹ':
                case 'ㇺ':
                case 'ㇻ':
                case 'ㇼ':
                case 'ㇽ':
                case 'ㇾ':
                case 'ㇿ':
                case '々':
                case '〻':

                // Hyphens
                case '‐':
                case '゠':
                case '–':
                case '〜':

                // Delimiters
                case '?':
                case '!':
                case '‼':
                case '⁇':
                case '⁈':
                case '⁉':

                // Mid-sentence punctuation
                // ・、:;,
                case '・':
                case '、':
                case ':':
                case ';':
                case ',':

                // Sentence-ending punctuation
                case '。':
                case '.':
                    return false;

                default:
                    return true;
            }
        }

        private static bool JpCanEndLine(char c)
        {
            switch (c)
            {
                case '(':
                case '[':
                case '｛':
                case '〔':
                case '〈':
                case '《':
                case '「':
                case '『':
                case '【':
                case '〘':
                case '〖':
                case '〝':
                case '‘':
                case '"':
                case '｟':
                case '«':
                    return false;

                default:
                    return true;
            }
        }
    }
}
