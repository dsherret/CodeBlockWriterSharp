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

        [Theory]
        [InlineData("\r\n", false)]
        [InlineData("\n", false)]
        [InlineData("\r", true)]
        [InlineData("random", true)]
        public void Constructor_VariousNewLines_OnlyAcceptsCertain(string newline, bool throws)
        {
            var ex = Record.Exception(() => new CodeBlockWriter(new Options
            {
                NewLine = newline
            }));

            if (throws)
                Assert.IsType<ArgumentException>(ex);
            else
                Assert.Null(ex);
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

        [Fact]
        public void InlineBlock_EmptyInlineBlock_Allows()
        {
            DoTest("someCall({\n});", writer =>
            {
                writer.Write("someCall(").InlineBlock().Write(");");
            });
        }

        [Fact]
        public void InlineBlock_TextInside_Writes()
        {
            DoTest("someCall({\n    console.log();\n});", writer =>
            {
                writer.Write("someCall(").InlineBlock(() => writer.Write("console.log();")).Write(");");
            });
        }

        [Fact]
        public void IndentBlock_TextInside_Indents()
        {
            DoTest("test\n    inside", writer =>
            {
                writer.Write("test").IndentBlock(() => writer.Write("inside"));
            });
        }

        [Fact]
        public void IndentBlock_FirstLine_IndentsNoNewLine()
        {
            DoTest("    inside", writer =>
            {
                writer.IndentBlock(() => writer.Write("inside"));
            });
        }

        [Fact]
        public void IndentBlock_LastNewLine_NoNewLine()
        {
            DoTest("    inside\ntest", writer =>
            {
                writer.IndentBlock(() => writer.WriteLine("inside")).Write("test");
            });
        }

        [Fact]
        public void IndentBlock_LastNewLineInside_NoNewLine()
        {
            DoTest("test\n    inside", writer =>
            {
                writer.WriteLine("test").IndentBlock(() => writer.Write("inside"));
            });
        }

        [Fact]
        public void IndentBlock_Nested_IndentsAll()
        {
            DoTest("test\n    inside\n        inside again\ntest", writer =>
            {
                writer.Write("test").IndentBlock(() =>
                {
                    writer.Write("inside").IndentBlock(() =>
                    {
                        writer.Write("inside again");
                    });
                }).Write("test");
            });
        }

        [Fact]
        public void IndentBlock_StringInside_NoIndentInString()
        {
            DoTest("block\n    const t = `\nt`;\n    const u = 1;", writer =>
            {
                writer.Write("block").IndentBlock(() =>
                {
                    writer.Write("const t = `\nt`;\nconst u = 1;");
                });
            });
        }

        [Fact]
        public void IndentBlock_CommentInside_Indents()
        {
            DoTest("block\n    const t = /*\n    const u = 1;*/", writer =>
            {
                writer.Write("block").IndentBlock(() =>
                {
                    writer.Write("const t = /*\nconst u = 1;*/");
                });
            });
        }

        [Theory]
        [InlineData("s\"y\"", new bool[] { false, false, true, true, false })]
        [InlineData("s'y'", new bool[] { false, false, true, true, false })]
        [InlineData("s`y`", new bool[] { false, false, true, true, false })]
        [InlineData("\"'`${}\"", new bool[] { false, true, true, true, true, true, true, false })]
        [InlineData("'\"`${}'", new bool[] { false, true, true, true, true, true, true, false })]
        [InlineData("`'\"`", new bool[] { false, true, true, true, false })]
        [InlineData("`y${t}`", new bool[] { false, true, true, true, false, false, true, false })]
        [InlineData("`${'t'}`", new bool[] { false, true, true, false, true, true, false, true, false })]
        [InlineData("`${\"t\"}`", new bool[] { false, true, true, false, true, true, false, true, false })]
        [InlineData("`${`t`}`", new bool[] { false, true, true, false, true, true, false, true, false })]
        [InlineData("`${`${t}`}`", new bool[] { false, true, true, false, true, true, false, false, true, false, true, false })]
        [InlineData("//'t'", new bool[] { false, false, false, false, false, false })]
        [InlineData("//t\n't'", new bool[] { false, false, false, false, false, true, true, false })]
        [InlineData("/*\n't'\n*/'t'", new bool[] { false, false, false, false, false, false, false, false, false, false, true, true, false })]
        [InlineData("/'test/", new bool[] { false, false, false, false, false, false, false, false })]
        [InlineData("/\"test/", new bool[] { false, false, false, false, false, false, false, false })]
        [InlineData("/`test/", new bool[] { false, false, false, false, false, false, false, false })]
        [InlineData("/`/'t'", new bool[] { false, false, false, false, true, true, false })]
        [InlineData("'\\''", new bool[] { false, true, true, true, false })]
        [InlineData("\"\\\"\"", new bool[] { false, true, true, true, false })]
        [InlineData("`\\``", new bool[] { false, true, true, true, false })]
        [InlineData("`\\${t}`", new bool[] { false, true, true, true, true, true, true, false })]
        public void IsInString_VariousInputs_Expected(string text, bool[] expectedValues)
        {
            Assert.Equal(expectedValues.Length, text.Length + 1);
            DoForWriters(writer =>
            {
                Assert.Equal(expectedValues[0], writer.IsInString());
                for (var i = 0; i < text.Length; i++)
                {
                    writer.Write(text[i].ToString());
                    Assert.Equal(expectedValues[i + 1], writer.IsInString());
                }
            });
        }

        [Theory]
        [InlineData(null, "test\n    test")]
        [InlineData(2, "test\n        test")]
        public void Indent_VariousInputs_Indents(int? times, string expected)
        {
            DoTest(expected, writer =>
            {
                writer.WriteLine("test");
                if (times.HasValue)
                    writer.Indent(times.Value);
                else
                    writer.Indent();

                writer.Write("test");
            });
        }

        [Theory]
        [InlineData("test", "test")]
        [InlineData("// test", "// test\n")]
        [InlineData("/* test", "/* test */")]
        [InlineData("/* test ", "/* test */")]
        [InlineData("/* test\n", "/* test\n*/")]
        public void CloseComment_VariousInputs_Closes(string startText, string expected)
        {
            DoTest(expected, writer =>
            {
                writer.Write(startText).CloseComment();
            });
        }

        [Fact]
        public void WithHangingIndentation_SingleIndentation_NewLine_QueuesAnIndentPlusOne()
        {
            var writer = new CodeBlockWriter();
            writer.SetIndentationLevel(2);
            writer.WithHangingIndentation(() =>
            {
                Assert.Equal(2, writer.GetIndentationLevel());
                writer.NewLine();
                Assert.Equal(3, writer.GetIndentationLevel());
            });
        }

        [Fact]
        public void WithHangingIndentation_SingleIndentation_WriteTextWithNewLine_QueuesAnIndentPlusOne()
        {
            var writer = new CodeBlockWriter();
            writer.SetIndentationLevel(2);
            writer.WithHangingIndentation(() =>
            {
                Assert.Equal(2, writer.GetIndentationLevel());
                writer.Write("test\ntest");
                Assert.Equal(3, writer.GetIndentationLevel());
            });
        }

        [Fact]
        public void WithHangingIndentation_NestedIndentationsOnSameLine_OnlyWritesOneIndent()
        {
            DoTest("(p: string\n    | number)", writer =>
            {
                writer.Write("(");
                writer.WithHangingIndentation(() =>
                {
                    writer.Write("p");
                    writer.WithHangingIndentation(() =>
                    {
                        writer.Write(": string\n| number");
                    });
                });
                writer.Write(")");
            });
        }

        [Fact]
        public void WithHangingIndentation_Block_Handles()
        {
            DoTest("{\n    }", writer =>
            {
                writer.WithHangingIndentation(() => writer.Block());
            });
        }

        private static void DoTest(string expected, Action<CodeBlockWriter> callback)
        {
            DoForWriters(DoForWriter);

            void DoForWriter(CodeBlockWriter writer)
            {
                callback(writer);
                Assert.Equal(expected.Replace("\n", writer.GetOptions().NewLine), writer.ToString());
            }
        }

        private static void DoForWriters(Action<CodeBlockWriter> callback)
        {
            DoForWriter(new CodeBlockWriter());
            DoForWriter(new CodeBlockWriter(new Options
            {
                NewLine = "\r\n"
            }));

            void DoForWriter(CodeBlockWriter writer)
            {
                callback(writer);
            }
        }
    }
}
