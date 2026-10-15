using System;
#if NET8_0_OR_GREATER
using System.Buffers;
#endif
using System.Collections.Generic;
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

    public class CodeBlockWriter
    {
        private struct IndentationState
        {
            public IndentationState(double current, double? queued)
            {
                Current = current;
                Queued = queued;
            }

            public double Current { get; }
            public double? Queued { get; }
        }

        private readonly string _indentationText;
        private readonly string _newLine;
        private readonly bool _useTabs;
        private readonly char _quoteChar;
        private readonly string _quoteText;
        private readonly int _indentNumberOfSpaces;
        private readonly StringBuilder _text = new StringBuilder();
        private readonly Stack<char> _stringCharStack = new Stack<char>();

        private double _currentIndentation;
        private double? _queuedIndentation;
        private bool _newLineOnNextWrite;
        private CommentChar? _currentCommentChar;
        private bool _isInRegEx;
        private bool _isOnFirstLineOfBlock;
        private char? _lastChar;

        public CodeBlockWriter(Options? options = null)
        {
            _newLine = options?.NewLine ?? "\n";
            _useTabs = options?.UseTabs ?? false;
            _indentNumberOfSpaces = options?.IndentNumberOfSpaces ?? 4;
            _indentationText = GetIndentationText(_useTabs, _indentNumberOfSpaces);
            _quoteChar = (options?.UseSingleQuote ?? false) ? '\'' : '"';
            _quoteText = _quoteChar.ToString();

            if (_newLine != "\r\n" && _newLine != "\n")
                throw new ArgumentException("Only \\r\\n and \\n is allowed for newline.");
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
        public CodeBlockWriter QueueIndentationLevel(int indentationLevel)
        {
            _queuedIndentation = GetIndentationLevelFromArg(indentationLevel);
            return this;
        }

        /// <summary>
        /// Queues the indentation level for the next lines written using the provided indentation text.
        /// </summary>
        /// <param name="indentationText">Gets the indentation level from the indentation text.</param>
        public CodeBlockWriter QueueIndentationLevel(string indentationText)
        {
            _queuedIndentation = GetIndentationLevelFromArg(indentationText);
            return this;
        }

        /// <summary>
        /// Writes the text within the provided action with hanging indentation.
        /// </summary>
        /// <param name="action">Writes to perform with hanging indentation.</param>
        public CodeBlockWriter WithHangingIndentation(Action action)
        {
            return WithQueuedIndentationLevel(GetIndentationLevel() + 1, action);
        }

        private CodeBlockWriter WithQueuedIndentationLevel(int indentationLevel, Action action)
        {
            var previousState = GetIndentationState();
            QueueIndentationLevel(indentationLevel);
            try
            {
                action();
            }
            finally
            {
                SetIndentationState(previousState);
            }
            return this;
        }

        /// <summary>
        /// Sets the current indentation level.
        /// </summary>
        /// <param name="indentationLevel">Indentation level to be at.</param>
        public CodeBlockWriter SetIndentationLevel(int indentationLevel)
        {
            _currentIndentation = GetIndentationLevelFromArg(indentationLevel);
            return this;
        }

        /// <summary>
        /// Sets the current indentation using the provided indentation text.
        /// </summary>
        /// <param name="indentationText">Gets the indentation level from the indentation text.</param>
        public CodeBlockWriter SetIndentationLevel(string indentationText)
        {
            _currentIndentation = GetIndentationLevelFromArg(indentationText);
            return this;
        }

        /// <summary>
        /// Sets the indentation level within the provided action and restores the writer's indentation state afterwards.
        /// </summary>
        /// <param name="indentationLevel">Indentation level to set.</param>
        /// <param name="action">Action to perform with the indentation.</param>
        /// <remarks>Restores the writer's indentation state after the action.</remarks>
        public CodeBlockWriter WithIndentationLevel(int indentationLevel, Action action)
        {
            return WithIndentationLevel(() => SetIndentationLevel(indentationLevel), action);
        }

        /// <summary>
        /// Sets the indentation level with the provided indentation text within the provided action and
        /// restores the writer's indentation state afterwards.
        /// </summary>
        /// <param name="indentationText">.</param>
        /// <param name="action">Action to perform with the indentation.</param>
        /// <remarks>Restores the writer's indentation state after the action.</remarks>
        public CodeBlockWriter WithIndentationLevel(string indentationText, Action action)
        {
            return WithIndentationLevel(() => SetIndentationLevel(indentationText), action);
        }

        private CodeBlockWriter WithIndentationLevel(Action setIndentationLevel, Action writeAction)
        {
            var previousState = GetIndentationState();
            setIndentationLevel();
            try
            {
                writeAction();
            }
            finally
            {
                SetIndentationState(previousState);
            }
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
        public CodeBlockWriter Block(Action block = null)
        {
            NewLineIfNewLineOnNextWrite();
            if (GetLength() > 0 && !IsLastNewLine())
                SpaceIfLastNot();
            InlineBlock(block);
            _newLineOnNextWrite = true;
            return this;
        }

        /// <summary>
        /// Writes an inline block with braces.
        /// </summary>
        /// <param name="block">Write using the writer within this block.</param>
        public CodeBlockWriter InlineBlock(Action block = null)
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
        public CodeBlockWriter IndentBlock(Action block)
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
            if (GetLastChar() != null)
                NewLineIfLastNot();
            _currentIndentation++;
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
        public CodeBlockWriter ConditionalWriteLine(bool? condition, Func<string> textFunc)
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
        public CodeBlockWriter ConditionalWriteLine(bool? condition, string text)
        {
            if (condition == true)
                WriteLine(text);

            return this;
        }

        /// <summary>
        /// Writes a line of text.
        /// </summary>
        /// <param name="text">Text to write.</param>
        public CodeBlockWriter WriteLine(string text)
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
        public CodeBlockWriter NewLineIfLastNot()
        {
            NewLineIfNewLineOnNextWrite();

            if (!IsLastNewLine())
                NewLine();

            return this;
        }

        /// <summary>
        /// Writes a blank line if the last written text was not a blank line.
        /// </summary>
        public CodeBlockWriter BlankLineIfLastNot()
        {
            if (!IsLastBlankLine())
                BlankLine();
            return this;
        }

        /// <summary>
        /// Writes a blank line if the condition is true.
        /// </summary>
        /// <param name="condition">Condition to evaluate.</param>
        public CodeBlockWriter ConditionalBlankLine(bool? condition)
        {
            if (condition == true)
                BlankLine();
            return this;
        }

        /// <summary>
        /// Writes a blank line.
        /// </summary>
        public CodeBlockWriter BlankLine()
        {
            return NewLineIfLastNot().NewLine();
        }

        /// <summary>
        /// Indents the code one level for the current line.
        /// </summary>
        /// <param name="times">Number of times to indent.</param>
        public CodeBlockWriter Indent(int times = 1)
        {
            NewLineIfNewLineOnNextWrite();
            for (var i = 0; i < times; i++)
                Write(_indentationText);
            return this;
        }

        /// <summary>
        /// Writes a newline if the condition is true.
        /// </summary>
        /// <param name="condition">Condition to evaluate.</param>
        public CodeBlockWriter ConditionalNewLine(bool? condition)
        {
            if (condition == true)
                NewLine();
            return this;
        }

        /// <summary>
        /// Writes a newline.
        /// </summary>
        public CodeBlockWriter NewLine()
        {
            _newLineOnNextWrite = false;
            BaseWriteNewline();
            return this;
        }

        /// <summary>
        /// Writes a quote character.
        /// </summary>
        public CodeBlockWriter Quote()
        {
            NewLineIfNewLineOnNextWrite();
            WriteIndentingNewLines(_quoteText);
            return this;
        }

        /// <summary>
        /// Writes text surrounded in quotes.
        /// </summary>
        /// <param name="text">Text to write.</param>
        public CodeBlockWriter Quote(string text)
        {
            NewLineIfNewLineOnNextWrite();
            // write each part separately to avoid allocating a string for the quoted text
            var escapedText = StringUtils.EscapeForWithinString(text, _quoteChar);
            WriteIndentingNewLines(_quoteText);
            if (escapedText.Length > 0)
                WriteIndentingNewLines(escapedText);
            WriteIndentingNewLines(_quoteText);
            return this;
        }

        /// <summary>
        /// Writes a space if the last character was not a space.
        /// </summary>
        public CodeBlockWriter SpaceIfLastNot()
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
        public CodeBlockWriter Space(int times = 1)
        {
            NewLineIfNewLineOnNextWrite();
            WriteIndentingNewLines(times == 1 ? " " : new string(' ', times));
            return this;
        }

        /// <summary>
        /// Writes a tab if the last character was not a tab.
        /// </summary>
        public CodeBlockWriter TabIfLastNot()
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
        public CodeBlockWriter Tab(int times = 1)
        {
            NewLineIfNewLineOnNextWrite();
            WriteIndentingNewLines(times == 1 ? "\t" : new string('\t', times));
            return this;
        }

        /// <summary>
        /// Conditionally writes text.
        /// </summary>
        /// <param name="condition">Condition to evaluate.</param>
        /// <param name="textFunc">A function that returns a string to write if the condition is true.</param>
        public CodeBlockWriter ConditionalWrite(bool? condition, Func<string> textFunc)
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
        public CodeBlockWriter ConditionalWrite(bool? condition, string text)
        {
            if (condition == true)
                Write(text);

            return this;
        }

        /// <summary>
        /// Writes the provided text.
        /// </summary>
        /// <param name="text">Text to write.</param>
        public CodeBlockWriter Write(string text)
        {
            NewLineIfNewLineOnNextWrite();
            WriteIndentingNewLines(text);
            return this;
        }

        /// <summary>
        /// Writes text to exist a comment if in a comment.
        /// </summary>
        public CodeBlockWriter CloseComment()
        {
            var commentChar = _currentCommentChar;

            switch (commentChar)
            {
                case CommentChar.Line:
                    NewLine();
                    break;
                case CommentChar.Star:
                    if (!IsLastNewLine())
                        SpaceIfLastNot();
                    Write("*/");
                    break;
            }

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
            return _stringCharStack.Count > 0 && _stringCharStack.Peek() != '{';
        }

        /// <summary>
        /// Gets if the last chars written were for a newline.
        /// </summary>
        public bool IsLastNewLine()
        {
            return _lastChar == '\n';
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
            return _lastChar;
        }

        /// <summary>
        /// Gets the writer's text.
        /// </summary>
        public override string ToString()
        {
            return _text.ToString();
        }

        private void WriteIndentingNewLines(string text)
        {
            text = text ?? "";
            if (text.Length == 0)
            {
                WriteIndividual(text, 0, 0);
                return;
            }

            // write each line separately (splitting on \r?\n) without allocating substrings
            var start = 0;
            while (true)
            {
                var newLineIndex = text.IndexOf('\n', start);
                if (newLineIndex == -1)
                {
                    if (start < text.Length)
                        WriteIndividual(text, start, text.Length);
                    break;
                }

                var end = newLineIndex > start && text[newLineIndex - 1] == '\r' ? newLineIndex - 1 : newLineIndex;
                if (start < end)
                    WriteIndividual(text, start, end);
                BaseWriteNewline();
                start = newLineIndex + 1;
            }
        }

        private void WriteIndividual(string text, int start, int end)
        {
            if (!IsInString())
            {
                var isAtStartOfLine = IsLastNewLine() || _text.Length == 0;
                if (isAtStartOfLine)
                    WriteIndentation();
            }

            UpdateInternalState(text, start, end);
            _text.Append(text, start, end - start);
            if (end > start)
                _lastChar = text[end - 1];
        }

        private void BaseWriteNewline()
        {
            if (_currentCommentChar == CommentChar.Line)
                _currentCommentChar = null;
            _text.Append(_newLine);
            _lastChar = '\n';
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

        /// <summary>
        /// Updates the internal state for the text between the start and end indexes that's about to
        /// be written.
        /// </summary>
        private void UpdateInternalState(string str, int start, int end)
        {
            var i = start;

            // the previously written text might mean the first char needs to be handled
            if (i < end && !IsCharToHandle(str[i]))
            {
                if (_lastChar == '/' || _lastChar == '\n')
                    UpdateInternalStateForChar(str, start, i);
                i++;
            }

            // this is a performance optimization to skip directly to the chars that may change the state
            while (i < end)
            {
                var index = IndexOfCharToHandle(str, i, end);
                if (index == -1)
                    break;

                UpdateInternalStateForChar(str, start, index);
                i = index + 1;

                var c = str[index];
                if ((c == '/' || c == '\n') && i < end && !IsCharToHandle(str[i]))
                {
                    UpdateInternalStateForChar(str, start, i);
                    i++;
                }
            }
        }

        private void UpdateInternalStateForChar(string str, int start, int i)
        {
            var currentChar = str[i];
            var pastChar = GetCharForUpdate(str, start, i - 1);
            var pastPastChar = GetCharForUpdate(str, start, i - 2);

            // handle regex
            if (_isInRegEx)
            {
                if (pastChar == '/' && pastPastChar != '\\' || pastChar == '\n')
                    _isInRegEx = false;
                else
                    return;
            }
            else if (!IsInString() && !IsInComment() && IsRegExStart() && !IsDivision(str, start, i - 1))
            {
                _isInRegEx = true;
                return;
            }

            // handle comments
            if (_currentCommentChar == null && pastChar == '/' && currentChar == '/')
                _currentCommentChar = CommentChar.Line;
            else if (_currentCommentChar == null && pastChar == '/' && currentChar == '*')
                _currentCommentChar = CommentChar.Star;
            else if (_currentCommentChar == CommentChar.Star && pastChar == '*' && currentChar == '/')
                _currentCommentChar = null;

            if (IsInComment())
                return;

            // handle strings
            var lastStringCharOnStack = _stringCharStack.Count > 0 ? _stringCharStack.Peek() : (char?)null;
            if (pastChar != '\\' && (currentChar == '"' || currentChar == '\'' || currentChar == '`'))
            {
                if (lastStringCharOnStack == currentChar)
                    _stringCharStack.Pop();
                else if (lastStringCharOnStack == '{' || lastStringCharOnStack == null)
                    _stringCharStack.Push(currentChar);
            }
            else if (pastPastChar != '\\' && pastChar == '$' && currentChar == '{' && lastStringCharOnStack == '`')
                _stringCharStack.Push(currentChar);
            else if (currentChar == '}' && lastStringCharOnStack == '{')
                _stringCharStack.Pop();

            bool IsRegExStart()
            {
                return pastChar == '/'
                    && currentChar != '/'
                    && currentChar != '*'
                    && pastPastChar != '*'
                    && pastPastChar != '/';
            }
        }

#if NET8_0_OR_GREATER
        private static readonly SearchValues<char> _charsToHandle = SearchValues.Create("/\\\n\r*\"'`{}");

        private static int IndexOfCharToHandle(string str, int start, int end)
        {
            var index = str.AsSpan(start, end - start).IndexOfAny(_charsToHandle);
            return index == -1 ? -1 : start + index;
        }
#else
        private static int IndexOfCharToHandle(string str, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                if (IsCharToHandle(str[i]))
                    return i;
            }
            return -1;
        }
#endif

        /// <summary>
        /// Gets if the char may change the internal state. Every other char won't change the state unless
        /// it comes directly after a slash or newline because that's where the start and end of a regex
        /// is detected.
        /// </summary>
        private static bool IsCharToHandle(char c)
        {
            switch (c)
            {
                case '/':
                case '\\':
                case '\n':
                case '\r':
                case '*':
                case '"':
                case '\'':
                case '`':
                case '{':
                case '}':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets if the slash at the provided index is a division operator based on what comes before it
        /// (ex. `a / b`, `(a) / b`, `a[0] / b`, `2 / b`, but not `return /a/`).
        /// </summary>
        private bool IsDivision(string str, int start, int slashIndex)
        {
            var index = slashIndex - 1;
            var c = GetCharForUpdate(str, start, index);
            while (c == ' ' || c == '\t')
                c = GetCharForUpdate(str, start, --index);

            if (c == ')' || c == ']')
                return true;
            if (c == null || !IsIdentifierChar(c.Value))
                return false;

            // a word longer than the longest keyword (instanceof) is an identifier
            var wordEnd = index;
            while (c != null && IsIdentifierChar(c.Value) && wordEnd - index < 11)
                c = GetCharForUpdate(str, start, --index);
            return !IsKeywordBeforeRegEx(str, start, index + 1, wordEnd - index);

            bool IsIdentifierChar(char value) => char.IsLetterOrDigit(value) || value == '_' || value == '$';
        }

        private bool IsKeywordBeforeRegEx(string str, int start, int wordStart, int length)
        {
            switch (length)
            {
                case 2:
                    return Is("in") || Is("of") || Is("do");
                case 3:
                    return Is("new");
                case 4:
                    return Is("void") || Is("case") || Is("else");
                case 5:
                    return Is("throw") || Is("yield") || Is("await");
                case 6:
                    return Is("return") || Is("typeof") || Is("delete");
                case 10:
                    return Is("instanceof");
                default:
                    return false;
            }

            bool Is(string keyword)
            {
                for (var i = 0; i < keyword.Length; i++)
                {
                    if (GetCharForUpdate(str, start, wordStart + i) != keyword[i])
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Gets the char at the provided index of the text being written, where an index before the
        /// start goes back into the already written text.
        /// </summary>
        private char? GetCharForUpdate(string str, int start, int index)
        {
            if (index >= start)
                return str[index];
            if (index == start - 1)
                return _lastChar;
            return _text.GetSafeChar(_text.Length - (start - index));
        }

        private void WriteIndentation()
        {
            var flooredIndentation = Math.Floor(_currentIndentation);
            var overflow = _currentIndentation - flooredIndentation;
            int count;
            if (_useTabs)
                count = (int)flooredIndentation + (overflow > 0.5 ? 1 : 0);
            else
                count = (int)flooredIndentation * _indentationText.Length + (int)Math.Round(_indentationText.Length * overflow);

            if (count > 0)
            {
                _text.Append(_useTabs ? '\t' : ' ', count);
                _lastChar = _useTabs ? '\t' : ' ';
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

        private void SetIndentationState(IndentationState state)
        {
            _currentIndentation = state.Current;
            _queuedIndentation = state.Queued;
        }

        private IndentationState GetIndentationState()
        {
            return new IndentationState(_currentIndentation, _queuedIndentation);
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
