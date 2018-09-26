using Xunit;

namespace CodeBlockWriterSharp.Tests
{
    public class StringUtilsTests
    {
        [Theory]
        [InlineData("'testing \"this\" out'", '\'', "\\'testing \"this\" out\\'")]
        [InlineData("\" testing \\\"this\\\" out\"", '"', "\\\" testing \\\\\"this\\\\\" out\\\"")]
        public void EscapeCharTests(string input, char character, string expected)
        {
            Assert.Equal(expected, StringUtils.EscapeChar(input, character));
        }

        [Theory]
        [InlineData("'testing\n this out'", "\\'testing\\\n this out\\'")]
        public void EscapeForWithinStringTests(string input, string expected)
        {
            Assert.Equal(expected, StringUtils.EscapeForWithinString(input, '\''));
        }
    }
}
