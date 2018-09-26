using System;
using Xunit;

namespace CodeBlockWriterSharp.Tests
{
    // todo: need to port over all the tests from the javascript project...

    public class CodeBlockWriterSharpTests
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

        private static void DoTest(string expected, Action<CodeBlockWriterSharp> callback)
        {
            DoForWriter(new CodeBlockWriterSharp());
            DoForWriter(new CodeBlockWriterSharp(new Options
            {
                NewLine = "\r\n"
            }));

            void DoForWriter(CodeBlockWriterSharp writer)
            {
                callback(writer);
                Assert.Equal(expected.Replace("\n", writer.GetOptions().NewLine), writer.ToString());
            }
        }
    }
}
