using System;
using Xunit;

namespace CodeBlockWriterSharp.Tests
{
    // todo: need to port over all the tests from the javascript project...

    public class CodeBlockWriterTests
    {
        [Fact]
        public void BasicTest()
        {
            DoTest("testing {\n    inner;\n}", writer =>
            {
                writer.Write("testing").Block(() =>
                {
                    writer.Write("inner;");
                });
            });
        }

        private static void DoTest(string expected, Action<CodeBlockWriter> callback)
        {
            DoForWriter(new CodeBlockWriter());
            DoForWriter(new CodeBlockWriter(new Options
            {
                NewLine = "\r\n"
            }));

            void DoForWriter(CodeBlockWriter writer)
            {
                callback(writer);
                Assert.Equal(expected.Replace("\n", writer.GetOptions().NewLine), writer.ToString());
            }
        }
    }
}
