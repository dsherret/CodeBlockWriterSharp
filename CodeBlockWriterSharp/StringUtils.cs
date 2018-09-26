using System.Text;
using System.Text.RegularExpressions;

namespace CodeBlockWriterSharp
{
    internal static class StringUtils
    {
        private static readonly Regex _newLine = new Regex(@"(\r?\n)");

        public static string EscapeForWithinString(string str, char quoteKind)
        {
            return _newLine.Replace(EscapeChar(str, quoteKind), "\\$1");
        }

        public static string EscapeChar(string str, char character)
        {
            var result = "";
            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] == character)
                    result += "\\";
                result += str[i];
            }
            return result;
        }

        public static char? GetSafeChar(this StringBuilder sb, int index)
        {
            if (index < 0 || index >= sb.Length)
                return null;
            return sb[index];
        }

        public static char? GetSafeChar(this string str, int index)
        {
            if (index < 0 || index >= str.Length)
                return null;
            return str[index];
        }
    }
}
