# CodeBlockWriterSharp

Code writer that assists with formatting and visualizing blocks of JavaScript or TypeScript code.

Conversion of [code-block-writer](https://github.com/dsherret/code-block-writer) to C# (.net standard and .net framework).

**Note:** This is a very rough first pass. I need to convert the tests to ensure it's working properly.

## Todo

* Convert tests from code-block-writer to c#.

## Install

Available on [nuget](https://www.nuget.org/packages/CodeBlockWriterSharp/).

```
dotnet add package CodeBlockWriterSharp
# or
Install-Package CodeBlockWriterSharp
# or
nuget install CodeBlockWriterSharp
```

## Example

```csharp
using CodeBlockWriterSharp;

// ...etc...

var writer = new CodeBlockWriter(new Options {
    // optional options
    NewLine = "\r\n",         // default: "\n"
    IndentNumberOfSpaces = 2, // default: 4
    UseTabs = false,          // default: false
    UseSingleQuote = true     // default: false
});

writer.Write("class MyClass extends OtherClass").Block(() => {
    writer.WriteLine("@MyDecorator(1, 2)");
    writer.Write("myMethod(myParam: any)").Block(() => {
        writer.Write("return this.post(").Quote("myArgument").Write(");");
    });
});

Console.WriteLine(writer.ToString());
```

Outputs (using "\r\n" for newlines):

```text
class MyClass extends OtherClass {
  @MyDecorator(1, 2)
  myMethod(myParam: any) {
    return this.post('myArgument');
  }
}
```

## Methods

* `Block(Action block = null)` - Indents all the code written within and surrounds it in braces.
* `InlineBlock(Action block = null)` - Same as block, but doesn't add a space before the first brace and doesn't add a newline at the end.
* `GetLength()` - Get the current number of characters.
* `WriteLine(string text)` - Writes some text and adds a newline.
* `NewLine()` - Writes a newline.
* `NewLineIfLastNot()` - Writes a newline if what was written last wasn't a newline.
* `BlankLine()` - Writes a blank line. Does not allow consecutive blank lines.
* `BlankLineIfLastNot()` - Writes a blank line if what was written last wasn't a blank line.
* `Quote()` - Writes a quote character.
* `Quote(string text)` - Writes text surrounded in quotes.
* `Indent(times = 1)` - Indents the current line. Optionally writes multiple indents when providing a number.
* `IndentBlock(Action block = null)` - Indents a block of code.
* `Space(int times = 1)` - Writes a space. Optionally writes multiple spaces when providing a number.
* `SpaceIfLastNot()` - Writes a space if the last was not a space.
* `Tab(int times = 1)` - Writes a tab. Optionally writes multiple tabs when providing a number.
* `TabIfLastNot()` - Writes a tab if the last was not a tab.
* `Write(string text)` - Writes some text.
* `ConditionalNewLine(bool condition)` - Writes a newline if the condition is matched.
* `ConditionalBlankLine(bool condition)` - Writes a blank line if the condition is matched.
* `ConditionalWrite(bool condition, string text)` - Writes if the condition is matched.
* `ConditionalWrite(bool condition, Func<string> textFunc)` - Writes if the condition is matched.
* `ConditionalWriteLine(bool condition, string text)` - Writes some text and adds a newline if the condition is matched.
* `ConditionalWriteLine(bool condition, Func<string> textFunc)` - Writes some text and adds a newline if the condition is matched.
* `SetIndentationLevel(int indentationLevel)` - Sets the current indentation level.
* `SetIndentationLevel(string whitespaceText)` - Sets the current indentation level based on the provided whitespace text.
* `GetIndentationLevel()` - Gets the current indentation level.
* `QueueIndentationLevel(int indentationLevel)` - Queues an indentation level to be used once a new line is written.
* `QueueIndentationLevel(string whitespaceText)` - Queues an indentation level to be used once a new line is written based on the provided whitespace text.
* `CloseComment()` - Writes text to exit a comment if in a comment.
* `IsInComment()` - Gets if the writer is currently in a comment.
* `IsAtStartOfFirstLineOfBlock()` - Gets if the writer is currently at the start of the first line of the text, block, or indentation block.
* `IsOnFirstLineOfBlock()` - Gets if the writer is currently on the first line of the text, block, or indentation block.
* `IsInString()` - Gets if the writer is currently in a string.
* `IsLastNewLine()` - Gets if the writer last wrote a newline.
* `IsLastBlankLine()` - Gets if the writer last wrote a blank line.
* `IsLastSpace()` - Gets if the writer last wrote a space.
* `IsLastTab()` - Gets if the writer last wrote a tab.
* `GetLastChar()` - Gets the last character written.
* `GetOptions()` - Gets the writer options.
* `ToString()` - Gets the string.

## Other Features

* Does not indent within strings.
* Escapes newlines within double and single quotes created with `.Quote(text)`.
