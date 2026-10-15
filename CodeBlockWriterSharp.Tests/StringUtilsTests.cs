using Xunit;

namespace CodeBlockWriterSharp.Tests
{
    public class StringUtilsTests
    {
        [Theory]
        [InlineData("'testing \"this\" out'", '\'', "\\'testing \"this\" out\\'")]
        [InlineData("\" testing \\\"this\\\" out\"", '"', "\\\" testing \\\\\"this\\\\\" out\\\"")]
        [InlineData("'testing\n this out'", '\'', "\\'testing\\\n this out\\'")]
        [InlineData("a\r\nb\rc\n\n", '"', "a\\\r\nb\rc\\\n\\\n")]
        [InlineData("\"\r\n'", '"', "\\\"\\\r\n'")]
        [InlineData("no escaping", '"', "no escaping")]
        [InlineData("", '"', "")]
        public void EscapeForWithinStringTests(string input, char quoteKind, string expected)
        {
            Assert.Equal(expected, StringUtils.EscapeForWithinString(input, quoteKind));
        }
    }
}
