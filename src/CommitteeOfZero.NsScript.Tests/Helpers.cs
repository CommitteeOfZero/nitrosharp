namespace CommitteeOfZero.NsScript.Tests
{
    public class Helpers
    {
        public static string RemoveNewLineCharacters(string s)
        {
            return s.Replace("\r\n", string.Empty).Replace("\n", string.Empty);
        }
    }
}
