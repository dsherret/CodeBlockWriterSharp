using System;
using Xunit;

namespace JavaScriptCodeBlockWriter.Tests
{
    // todo: need to port over all the tests from the javascript project...

    public class JavaScriptCodeBlockWriterTests
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

        private static void DoTest(string expected, Action<JavaScriptCodeBlockWriter> callback)
        {
            DoForWriter(new JavaScriptCodeBlockWriter());
            DoForWriter(new JavaScriptCodeBlockWriter(new Options
            {
                NewLine = "\r\n"
            }));

            void DoForWriter(JavaScriptCodeBlockWriter writer)
            {
                callback(writer);
                Assert.Equal(expected.Replace("\n", writer.GetOptions().NewLine), writer.ToString());
            }
        }
    }
}
