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

        [Fact]
        public void WriteLine_DefaultConstructor_WritesNewline()
        {
            var writer = new CodeBlockWriter();
            writer.WriteLine("test");
            Assert.Equal("test\n", writer.ToString());
        }

        [Theory]
        [InlineData("a", "a")]
        [InlineData("test", "test")]
        [InlineData(null, "")]
        [InlineData("\n\ntest\n\n", "\n\ntest\n\n")]
        public void Write_VariousInputs_Writes(string textToWrite, string expected)
        {
            DoTest(expected, writer =>
            {
                writer.Write(textToWrite);
            });
        }

        [Theory]
        [InlineData("test\ntest", "    test\n    test")]
        [InlineData("test\n\ntest", "    test\n\n    test")]
        public void Write_Indented_Writes(string textToWrite, string expected)
        {
            DoTest(expected, writer =>
            {
                writer.SetIndentationLevel(1);
                writer.Write(textToWrite);
            });
        }

        [Fact]
        public void Write_EmptyStringAtStartOfLine_Indents()
        {
            DoTest("    test\n    ", writer =>
            {
                writer.SetIndentationLevel(1);
                writer.WriteLine("test");
                writer.Write("");
            });
        }

        [Fact]
        public void Block_NoArgument_Writes()
        {
            DoTest("test {\n}", writer => writer.Write("test").Block());
        }

        [Fact]
        public void Block_InsideBlock_Writes()
        {
            DoTest("test {\n    inside\n}", writer =>
            {
                writer.Write("test").Block(() => writer.Write("inside"));
            });
        }

        [Fact]
        public void Block_BlockWithinBlock_Writes()
        {
            DoTest("test {\n    inside {\n        inside again\n    }\n}", writer =>
            {
                writer.Write("test").Block(() =>
                {
                    writer.Write("inside").Block(() => writer.Write("inside again"));
                });
            });
        }

        [Fact]
        public void Block_SpaceBeforeBlock_DoesNotWriteSpace()
        {
            DoTest("test {\n    inside\n}", writer => writer.Write("test ").Block(() => writer.Write("inside")));
        }

        [Fact]
        public void Block_NewLineBeforeBlock_DoesNotWriteSpace()
        {
            DoTest("test\n{\n}", writer => writer.WriteLine("test").Block());
        }

        [Fact]
        public void Block_FirstLineBlock_DoesNotWriteSpace()
        {
            DoTest("{\n}", writer => writer.Block());
        }

        [Fact]
        public void Block_NewLineInBlock_DoesNotWriteExtraNewLine()
        {
            DoTest("{\n    inside\n}", writer => writer.Block(() => writer.WriteLine("inside")));
        }

        [Fact]
        public void Block_TextAfterBlock_AddsOnNewLine()
        {
            DoTest("{\n}\n ", writer => writer.Block().Space());
        }

        [Fact]
        public void Block_ConditionalCallsFalse_DoesNotAddNewLine()
        {
            DoTest("{\n}", writer =>
            {
                writer.Block()
                    .ConditionalWrite(false, "test")
                    .ConditionalWriteLine(false, "test")
                    .ConditionalNewLine(false)
                    .ConditionalBlankLine(false);
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
