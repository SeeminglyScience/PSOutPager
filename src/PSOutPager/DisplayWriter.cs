using System;
using System.Globalization;
using System.Management.Automation;

namespace PSOutPager
{
    public abstract class DisplayWriter : IDisposable
    {
        private bool _isDisposed;

        private int _columns;

        private int _lines;

        private int _width;

        private int _height;

        private int _messageLine;

        private bool _isMessageWritten;

        private Buffer _buffer;

        protected readonly Memory<char> CoreNewLine;

        protected DisplayWriter()
        {
            CoreNewLine = NewLine.ToCharArray().AsMemory();
            _width = Console.BufferWidth - 1;
            _height = Console.WindowHeight - 1;
            _buffer = new Buffer(_width * _height);
        }

        protected virtual string NewLine => Environment.NewLine;

        private protected abstract unsafe void InternalWrite(ReadOnlySpan<char> buffer);

        private protected virtual void InternalWrite(string value) => InternalWrite(value.AsSpan());

        internal protected virtual void InternalWriteLine() => InternalWrite(CoreNewLine.Span);

        private unsafe void ProcessChars(char* buffer, int length)
        {
            char* current = buffer;
            int bytesRemaining = length;
            while (bytesRemaining > 0)
            {
                bool shouldMoveColumn = TryIncrementColumn(
                    current,
                    bytesRemaining,
                    out int charsProcessed,
                    out bool shouldSkip);

                if (!shouldMoveColumn)
                {
                    if (shouldSkip)
                    {
                        WritePendingAndAdvance(ref buffer, current);
                    }
                    else
                    {
                        current--;
                        WritePendingAndAdvance(ref buffer, current);
                    }

                    WriteLine();

                    if (shouldSkip)
                    {
                        buffer += charsProcessed;
                        current += charsProcessed;
                        bytesRemaining -= charsProcessed;
                        continue;
                    }

                    current++;
                    continue;
                }

                if (shouldSkip)
                {
                    WritePendingAndAdvance(ref buffer, current);
                    buffer += charsProcessed;
                }

                current += charsProcessed;
                bytesRemaining -= charsProcessed;
            }

            WritePendingAndAdvance(ref buffer, current);
        }

        private unsafe void WritePendingAndAdvance(ref char* start, char* finish)
        {
            int length = (int)(finish - start);
            if (length > 0)
            {
                _buffer.Write(start, length);
            }

            start = finish;
        }

        private unsafe bool TryIncrementColumn(
            char* buffer,
            int length,
            out int charsProcessed,
            out bool shouldSkip)
        {
            charsProcessed = 1;
            shouldSkip = false;
            if (length == 0)
            {
                return true;
            }

            char c = *buffer;
            if (c == '\r')
            {
                shouldSkip = true;
                if (length == 1 || buffer[1] != '\n')
                {
                    return true;
                }

                charsProcessed = 2;
                return false;
            }

            if (c == '\n')
            {
                shouldSkip = true;
                return false;
            }

            if (c == '\x1b')
            {
                if (AnsiEscapeParser.TryParseEscape(buffer, length, out charsProcessed))
                {
                    return true;
                }

                // If we couldn't parse the escape sequence then treat it like
                // every other character.
                charsProcessed = 1;
            }

            if (c == '\b')
            {
                if (_columns > 0) _columns--;
                return true;
            }

            if (_columns - 1 == _width)
            {
                return false;
            }

            _columns++;
            return true;
        }

        public virtual unsafe void Write(char c) => ProcessChars(&c, 1);

        public virtual unsafe void Write(string value)
        {
            fixed (char* c = value)
            {
                ProcessChars(c, value.Length);
            }
        }

        public virtual void WriteLine()
        {
            _columns = 0;
            if (_lines < _height)
            {
                Flush();
                InternalWriteLine();
                _lines++;
                return;
            }

            MaybePrepareBuffer();

            bool oldValue = Console.TreatControlCAsInput;
            Console.TreatControlCAsInput = true;
            try
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.J:
                    case ConsoleKey.Enter: break;

                    case ConsoleKey.Spacebar: _lines = 1; break;

                    case (ConsoleKey)0x03:
                    case ConsoleKey.Q: throw new PipelineStoppedException();
                    case ConsoleKey.C:
                    {
                        if ((key.Modifiers & ConsoleModifiers.Control) != 0)
                        {
                            throw new PipelineStoppedException();
                        }

                        break;
                    }
                }

                InternalWriteLine();
                Flush();
            }
            finally
            {
                Console.TreatControlCAsInput = oldValue;
            }
        }

        private void MaybePrepareBuffer()
        {
            if (_isMessageWritten)
            {
                return;
            }

            InternalWrite("\x1b[?25l");
            InternalWriteLine();
            InternalWrite("\x1bM");
            ReadOnlySpan<char> saveCursor = stackalloc char[] { '\x1b', '7' };
            InternalWrite(saveCursor);
            InternalWrite($"\x1b[1;{_height}r");
            InternalWrite($"\x1b[{_height + 1};1H");
            InternalWrite("<SPACE> next page; <CR> next line; Q quit");
            ReadOnlySpan<char> restoreCursor = stackalloc char[] { '\x1b', '8' };
            InternalWrite(restoreCursor);
            _isMessageWritten = true;
        }

        public virtual void Write(string format, object arg0) => Write(string.Format(CultureInfo.InvariantCulture, format, arg0));

        public virtual void Write(string format, object arg0, object arg1) => Write(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1));

        public virtual void Write(string format, object arg0, object arg1, object arg2) => Write(string.Format(CultureInfo.InvariantCulture, format, arg0, arg2));

        public virtual void Write(string format, params object[] args) => Write(string.Format(CultureInfo.InvariantCulture, format, args));

        public virtual void Write(IFormatProvider provider, string format, object arg0) => Write(string.Format(provider, format, arg0));

        public virtual void Write(IFormatProvider provider, string format, object arg0, object arg1) => Write(string.Format(provider, format, arg0, arg1));

        public virtual void Write(IFormatProvider provider, string format, object arg0, object arg1, object arg2) => Write(string.Format(provider, format, arg0, arg2));

        public virtual void Write(IFormatProvider provider, string format, params object[] args) => Write(string.Format(provider, format, args));

        public virtual unsafe void Write(char* ptr, int length) => ProcessChars(ptr, length);

        public virtual unsafe void Write(ReadOnlySpan<char> buffer)
        {
            fixed (char* ptr = buffer)
            {
                Write(ptr, buffer.Length);
            }
        }

        public virtual void Flush()
        {
            if (_buffer.Offset == 0)
            {
                return;
            }

            InternalWrite(_buffer.AsSpan());
            _buffer.Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                InternalWrite("\x1b[!p");
                InternalWrite("\x1b[?25h");
                InternalWriteLine();
                InternalWriteLine();
            }

            _isDisposed = true;
        }

        public void Dispose() => Dispose(true);
    }
}
