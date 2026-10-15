using System.Text;

namespace CodeBlockWriterSharp
{
    internal static class StringUtils
    {
        private static readonly char[] _doubleQuoteCharsToEscape = { '"', '\n' };
        private static readonly char[] _singleQuoteCharsToEscape = { '\'', '\n' };

        /// <summary>
        /// Escapes the quote char and newlines (\r?\n) with a backslash.
        /// </summary>
        public static string EscapeForWithinString(string str, char quoteKind)
        {
            var charsToEscape = quoteKind == '"' ? _doubleQuoteCharsToEscape
                : quoteKind == '\'' ? _singleQuoteCharsToEscape
                : new[] { quoteKind, '\n' };
            var index = str.IndexOfAny(charsToEscape);
            if (index == -1)
                return str;

            var result = new StringBuilder(str.Length + 16);
            var start = 0;
            while (index != -1)
            {
                // the backslash goes before the \r in \r\n
                var escapeIndex = str[index] == '\n' && index > 0 && str[index - 1] == '\r' ? index - 1 : index;
                result.Append(str, start, escapeIndex - start).Append('\\');
                start = escapeIndex;
                index = index + 1 < str.Length ? str.IndexOfAny(charsToEscape, index + 1) : -1;
            }
            result.Append(str, start, str.Length - start);
            return result.ToString();
        }

        public static char? GetSafeChar(this StringBuilder sb, int index)
        {
            if (index < 0 || index >= sb.Length)
                return null;
            return sb[index];
        }
    }
}
