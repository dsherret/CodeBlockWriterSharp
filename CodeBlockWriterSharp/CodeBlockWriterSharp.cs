using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeBlockWriterSharp
{
    public struct Options
    {
        public string NewLine;
        public int? IndentNumberOfSpaces;
        public bool? UseTabs;
        public bool? UseSingleQuote;
    }

    public class CodeBlockWriterSharp
    {
        private readonly string _indentationText;
        private readonly string _newLine;
        private readonly bool _useTabs;
        private readonly char _quoteChar;
        private readonly int _indentNumberOfSpaces;
        private readonly StringBuilder _text = new StringBuilder();
        private readonly Stack<char> _stringCharStack = new Stack<char>();

        private double _currentIndentation;
        private double? _queuedIndentation;
        private bool _newLineOnNextWrite;
        private CommentChar? _currentCommentChar;
        private bool _isInRegEx;
        private bool _isOnFirstLineOfBlock;

        public CodeBlockWriterSharp(Options? options = null)
        {
            _newLine = options?.NewLine ?? "\n";
            _useTabs = options?.UseTabs ?? false;
            _indentNumberOfSpaces = options?.IndentNumberOfSpaces ?? 4;
            _indentationText = GetIndentationText(_useTabs, _indentNumberOfSpaces);
            _quoteChar = (options?.UseSingleQuote ?? false) ? '\'' : '"';
        }

        /// <summary>
        /// Gets the options.
        /// </summary>
        public Options GetOptions()
        {
            return new Options
            {
                IndentNumberOfSpaces = _indentNumberOfSpaces,
                NewLine = _newLine,
                UseTabs = _useTabs,
                UseSingleQuote = _quoteChar == '\''
            };
        }

        /// <summary>
        /// Queues the indentation level for the next lines written.
        /// </summary>
        /// <param name="indentationLevel">Indentation level to queue.</param>
        public CodeBlockWriterSharp QueueIndentationLevel(int indentationLevel)
        {
            _queuedIndentation = GetIndentationLevelFromArg(indentationLevel);
            return this;
        }

        /// <summary>
        /// Queues the indentation level for the next lines written using the provided indentation text.
        /// </summary>
        /// <param name="indentationText">Gets the indentation level from the indentation text.</param>
        public CodeBlockWriterSharp QueueIndentationLevel(string indentationText)
        {
            _queuedIndentation = GetIndentationLevelFromArg(indentationText);
            return this;
        }

        /// <summary>
        /// Sets the current indentation level.
        /// </summary>
        /// <param name="indentationLevel">Indentation level to be at.</param>
        public CodeBlockWriterSharp SetIndentationLevel(int indentationLevel)
        {
            _currentIndentation = GetIndentationLevelFromArg(indentationLevel);
            return this;
        }

        /// <summary>
        /// Sets the current indentation using the provided indentation text.
        /// </summary>
        /// <param name="indentationText">Gets the indentation level from the indentation text.</param>
        public CodeBlockWriterSharp SetIndentationLevel(string indentationText)
        {
            _currentIndentation = GetIndentationLevelFromArg(indentationText);
            return this;
        }

        /// <summary>
        /// Gets the indentation level.
        /// </summary>
        public int GetIndentationLevel()
        {
            // todo: how to handle when this is 0.75 or something like that?
            return (int)_currentIndentation;
        }

        /// <summary>
        /// Writes a block using braces.
        /// </summary>
        /// <param name="block">Write using the writer within this block.</param>
        public CodeBlockWriterSharp Block(Action block = null)
        {
            NewLineIfNewLineOnNextWrite();
            SpaceIfLastNot();
            InlineBlock(block);
            _newLineOnNextWrite = true;
            return this;
        }

        /// <summary>
        /// Writes an inline block with braces.
        /// </summary>
        /// <param name="block">Write using the writer within this block.</param>
        public CodeBlockWriterSharp InlineBlock(Action block = null)
        {
            NewLineIfNewLineOnNextWrite();
            Write("{");
            IndentBlockInternal(block);
            NewLineIfLastNot().Write("}");

            return this;
        }

        /// <summary>
        /// Indents a block of code.
        /// </summary>
        /// <param name="block">Block to indent.</param>
        public CodeBlockWriterSharp IndentBlock(Action block)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            IndentBlockInternal(block);
            if (!IsLastNewLine())
                _newLineOnNextWrite = true;
            return this;
        }

        private void IndentBlockInternal(Action block)
        {
            _currentIndentation++;
            if (GetLastChar() != null)
                NewLineIfLastNot();
            _isOnFirstLineOfBlock = true;
            block?.Invoke();
            _isOnFirstLineOfBlock = false;
            _currentIndentation = Math.Max(0, _currentIndentation - 1);
        }

        /// <summary>
        /// Conditionally writes a line of text.
        /// </summary>
        /// <param name="condition">Condition to evaluate.</param>
        /// <param name="textFunc">A function that returns a string to write if the condition is true.</param>
        public CodeBlockWriterSharp ConditionalWriteLine(bool? condition, Func<string> textFunc)
        {
            if (condition == true)
                WriteLine(textFunc());
            return this;
        }

        /// <summary>
        /// Conditionally writes a line of text.
        /// </summary>
        /// <param name="condition">Condition to evaluate.</param>
        /// <param name="text">Text to write if the condition is true.</param>
        public CodeBlockWriterSharp ConditionalWriteLine(bool? condition, string text)
        {
            if (condition == true)
                WriteLine(text);

            return this;
        }

        /// <summary>
        /// Writes a line of text.
        /// </summary>
        /// <param name="text">Text to write.</param>
        public CodeBlockWriterSharp WriteLine(string text)
        {
            NewLineIfNewLineOnNextWrite();
            if (_text.Length > 0)
                NewLineIfLastNot();
            WriteIndentingNewLines(text);
            NewLine();

            return this;
        }

        /// <summary>
        /// Writes a newline if the last line was not a newline.
        /// </summary>
        public CodeBlockWriterSharp NewLineIfLastNot()
        {
            NewLineIfNewLineOnNextWrite();

            if (!IsLastNewLine())
                NewLine();

            return this;
        }

        /// <summary>
        /// Writes a blank line if the last written text was not a blank line.
        /// </summary>
        public CodeBlockWriterSharp BlankLineIfLastNot()
        {
            if (!IsLastBlankLine())
                BlankLine();
            return this;
        }

        /// <summary>
        /// Writes a blank line if the condition is true.
        /// </summary>
        /// <param name="condition">Condition to evaluate.</param>
        public CodeBlockWriterSharp ConditionalBlankLine(bool? condition)
        {
            if (condition == true)
                BlankLine();
            return this;
        }

        /// <summary>
        /// Writes a blank line.
        /// </summary>
        public CodeBlockWriterSharp BlankLine()
        {
            return NewLineIfLastNot().NewLine();
        }

        /// <summary>
        /// Indents the code one level for the current line.
        /// </summary>
        public CodeBlockWriterSharp Indent()
        {
            NewLineIfNewLineOnNextWrite();
            return Write(_indentationText);
        }

        /// <summary>
        /// Writes a newline if the condition is true.
        /// </summary>
        /// <param name="condition">Condition to evaluate.</param>
        public CodeBlockWriterSharp ConditionalNewLine(bool? condition)
        {
            if (condition == true)
                NewLine();
            return this;
        }

        /// <summary>
        /// Writes a newline.
        /// </summary>
        public CodeBlockWriterSharp NewLine()
        {
            _newLineOnNextWrite = false;
            BaseWriteNewline();
            return this;
        }

        /// <summary>
        /// Writes a quote character.
        /// </summary>
        public CodeBlockWriterSharp Quote()
        {
            NewLineIfNewLineOnNextWrite();
            WriteIndentingNewLines(_quoteChar.ToString());
            return this;
        }

        /// <summary>
        /// Writes text surrounded in quotes.
        /// </summary>
        /// <param name="text">Text to write.</param>
        public CodeBlockWriterSharp Quote(string text)
        {
            NewLineIfNewLineOnNextWrite();
            WriteIndentingNewLines(_quoteChar + StringUtils.EscapeForWithinString(text, _quoteChar) + _quoteChar);
            return this;
        }

        /// <summary>
        /// Writes a space if the last character was not a space.
        /// </summary>
        public CodeBlockWriterSharp SpaceIfLastNot()
        {
            NewLineIfNewLineOnNextWrite();

            if (!IsLastSpace())
                WriteIndentingNewLines(" ");

            return this;
        }

        /// <summary>
        /// Writes a space.
        /// </summary>
        /// <param name="times">Number of times to write a space.</param>
        public CodeBlockWriterSharp Space(int times = 1)
        {
            NewLineIfNewLineOnNextWrite();
            WriteIndentingNewLines(new string(' ', times));
            return this;
        }

        /// <summary>
        /// Writes a tab if the last character was not a tab.
        /// </summary>
        public CodeBlockWriterSharp TabIfLastNot()
        {
            NewLineIfNewLineOnNextWrite();

            if (!IsLastTab())
                WriteIndentingNewLines("\t");

            return this;
        }

        /// <summary>
        /// Writes a tab.
        /// </summary>
        /// <param name="times">Number of times to write a tab.</param>
        public CodeBlockWriterSharp Tab(int times = 1)
        {
            NewLineIfNewLineOnNextWrite();
            WriteIndentingNewLines(new string('\t', times));
            return this;
        }

        /// <summary>
        /// Conditionally writes text.
        /// </summary>
        /// <param name="condition">Condition to evaluate.</param>
        /// <param name="textFunc">A function that returns a string to write if the condition is true.</param>
        public CodeBlockWriterSharp ConditionalWrite(bool? condition, Func<string> textFunc)
        {
            if (condition == true)
                Write(textFunc());

            return this;
        }

        /// <summary>
        /// Conditionally writes text.
        /// </summary>
        /// <param name="condition">Condition to evaluate.</param>
        /// <param name="text">Text to write if the condition is true.</param>
        public CodeBlockWriterSharp ConditionalWrite(bool? condition, string text)
        {
            if (condition == true)
                Write(text);

            return this;
        }

        /// <summary>
        /// Writes the provided text.
        /// </summary>
        /// <param name="text">Text to write.</param>
        public CodeBlockWriterSharp Write(string text)
        {
            NewLineIfNewLineOnNextWrite();
            WriteIndentingNewLines(text);
            return this;
        }

        /// <summary>
        /// Gets the length of the string in the printer.
        /// </summary>
        public int GetLength()
        {
            return _text.Length;
        }

        /// <summary>
        /// Gets if the writer is currently in a comment.
        /// </summary>
        public bool IsInComment()
        {
            return _currentCommentChar != null;
        }

        /// <summary>
        /// Gets if the writer is currently at the start of the first line of the text, block, or indentation block.
        /// </summary>
        public bool IsAtStartOfFirstLineOfBlock()
        {
            return IsOnFirstLineOfBlock() && (IsLastNewLine() || GetLastChar() == null);
        }

        /// <summary>
        /// Gets if the writer is currently on the first line of the text, block, or indentation block.
        /// </summary>
        public bool IsOnFirstLineOfBlock()
        {
            return _isOnFirstLineOfBlock;
        }

        /// <summary>
        /// Gets if the writer is currently in a string.
        /// </summary>
        public bool IsInString()
        {
            return _stringCharStack.Any() && _stringCharStack.Peek() != '{';
        }

        /// <summary>
        /// Gets if the last chars written were for a newline.
        /// </summary>
        public bool IsLastNewLine()
        {
            return _text.GetSafeChar(_text.Length - 1) == '\n';
        }

        /// <summary>
        /// Gets if the last chars written were for a blank line.
        /// </summary>
        public bool IsLastBlankLine()
        {
            var foundCount = 0;
            for (var i = _text.Length - 1; i >= 0; i--)
            {
                var currentChar = _text[i];
                if (currentChar == '\n')
                {
                    foundCount++;
                    if (foundCount == 2)
                        return true;
                }
                else if (currentChar != '\r')
                    return false;
            }
            return false;
        }

        /// <summary>
        /// Gets if the last char written was a space.
        /// </summary>
        public bool IsLastSpace()
        {
            return GetLastChar() == ' ';
        }

        /// <summary>
        /// Gets if the last char written was a tab.
        /// </summary>
        public bool IsLastTab()
        {
            return GetLastChar() == '\t';
        }

        /// <summary>
        /// Gets the last char written.
        /// </summary>
        public char? GetLastChar()
        {
            return _text.GetSafeChar(_text.Length - 1);
        }

        /// <summary>
        /// Gets the writer's text.
        /// </summary>
        public override string ToString()
        {
            return _text.ToString();
        }

        private static readonly Regex _newLineRegex = new Regex("\r?\n");

        private void WriteIndentingNewLines(string text)
        {
            text = text ?? "";
            if (text.Length == 0)
            {
                WriteIndividual("");
                return;
            }

            var items = _newLineRegex.Split(text);
            for (var i = 0; i < items.Length; i++)
            {
                if (i > 0)
                    BaseWriteNewline();

                if (items[i].Length == 0)
                    continue;

                WriteIndividual(items[i]);
            }

            void WriteIndividual(string s)
            {
                if (!IsInString())
                {
                    var isAtStartOfLine = IsLastNewLine() || _text.Length == 0;
                    if (isAtStartOfLine)
                        WriteIndentation();
                }

                UpdateInternalState(s);
                _text.Append(s);
                DequeueQueuedIndentation();
            }
        }

        private void BaseWriteNewline()
        {
            if (_currentCommentChar == CommentChar.Line)
                _currentCommentChar = null;
            _text.Append(_newLine);
            _isOnFirstLineOfBlock = false;
            DequeueQueuedIndentation();
        }

        private void DequeueQueuedIndentation()
        {
            if (_queuedIndentation == null)
                return;

            _currentIndentation = _queuedIndentation.Value;
            _queuedIndentation = null;
        }

        private void UpdateInternalState(string str)
        {
            for (var i = 0; i < str.Length; i++)
            {
                var currentChar = str[i];
                var pastChar = i == 0 ? _text.GetSafeChar(_text.Length - 1) : str.GetSafeChar(i - 1);
                var pastPastChar = i == 0 ? _text.GetSafeChar(_text.Length - 2) : str.GetSafeChar(i - 2);

                // handle regex
                if (_isInRegEx)
                {
                    if (pastChar == '/' && pastPastChar != '\\' || pastChar == '\n')
                        _isInRegEx = false;
                    else
                        continue;
                }
                else if (!IsInString() && !IsInComment() && IsRegExStart(pastPastChar, pastChar, currentChar))
                {
                    _isInRegEx = true;
                    continue;
                }

                // handle comments
                if (_currentCommentChar == null && pastChar == '/' && currentChar == '/')
                    _currentCommentChar = CommentChar.Line;
                else if (_currentCommentChar == null && pastChar == '/' && currentChar == '*')
                    _currentCommentChar = CommentChar.Star;
                else if (_currentCommentChar == CommentChar.Star && pastChar == '*' && currentChar == '/')
                    _currentCommentChar = null;

                if (IsInComment())
                    continue;

                // handle strings
                var lastStringCharOnStack = _stringCharStack.Any() ? _stringCharStack.Peek() : (char?)null;
                if (currentChar == '"' || currentChar == '\'' || currentChar == '`')
                {
                    if (lastStringCharOnStack == currentChar)
                        _stringCharStack.Pop();
                    else if (lastStringCharOnStack == '{' || lastStringCharOnStack == null)
                        _stringCharStack.Push(currentChar);
                }
                else if (pastChar == '$' && currentChar == '{' && lastStringCharOnStack == '`')
                    _stringCharStack.Push(currentChar);
                else if (currentChar == '}' && lastStringCharOnStack == '{')
                    _stringCharStack.Pop();
            }

            bool IsRegExStart(char? pastPastChar, char? pastChar, char currentChar)
            {
                return pastChar == '/'
                    && currentChar != '/'
                    && currentChar != '*'
                    && pastPastChar != '*'
                    && pastPastChar != '/';
            }
        }

        private void WriteIndentation()
        {
            var flooredIndentation = Math.Floor(_currentIndentation);
            for (var i = 0; i < (int)flooredIndentation; i++)
                _text.Append(_indentationText);

            var overflow = _currentIndentation - flooredIndentation;
            if (_useTabs)
            {
                if (overflow > 0.5)
                    _text.Append(_indentationText);
            }
            else
            {
                var portion = Math.Round(_indentationText.Length * overflow);
                for (var i = 0; i < portion; i++)
                    _text.Append(_indentationText[i]);
            }
        }

        private void NewLineIfNewLineOnNextWrite()
        {
            if (!_newLineOnNextWrite)
                return;
            _newLineOnNextWrite = false;
            NewLine();
        }

        private double GetIndentationLevelFromArg(int count)
        {
            if (count < 0)
                throw new Exception("Passed in indentation level should be greater than or equal to 0.");
            return count;
        }

        private static readonly Regex _spacesOrTabs = new Regex("^[ \t]*$");

        private double GetIndentationLevelFromArg(string text)
        {
            if (!_spacesOrTabs.IsMatch(text))
                throw new ArgumentException("Provided string must be empty or only contain spaces or tabs.", nameof(text));

            GetSpacesAndTabsCount(text, out var spacesCount, out var tabsCount);
            return tabsCount + spacesCount / (double)_indentNumberOfSpaces;
        }

        private static string GetIndentationText(bool useTabs, int numberSpaces)
        {
            return useTabs ? "\t" : new string(' ', numberSpaces);
        }

        private static void GetSpacesAndTabsCount(string str, out int spacesCount, out int tabsCount)
        {
            spacesCount = 0;
            tabsCount = 0;

            foreach (var c in str)
            {
                switch (c)
                {
                    case '\t':
                        tabsCount++;
                        break;
                    case ' ':
                        spacesCount++;
                        break;
                }
            }
        }
    }
}
