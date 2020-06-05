using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PSOutPager
{
    [DebuggerDisplay("{ToString()}")]
    internal unsafe class Buffer : IDisposable
    {
        private Block _block;

        private byte* _currentPointer;

        private bool _isDisposed;

        public Buffer(int capacity)
        {
            _block = new Block(capacity);
            _currentPointer = _block.StartPointer;
        }

        ~Buffer() => Dispose(false);

        public long Offset => _currentPointer - _block.StartPointer;

        public long RemainingBytes => _block.EndPointer - _currentPointer;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _block.Dispose();
            _currentPointer = default;
            _isDisposed = true;
        }

        public void Write(bool value) => Write(value ? "True" : "False");

        public void Write(float value) => Write(value.ToString());

        public void Write(double value) => Write(value.ToString());

        public void Write(sbyte value) => WriteNumber(value);

        public void Write(short value) => WriteNumber(value);

        public void Write(int value) => WriteNumber(value);

        public void Write(long value) => WriteNumber(value);

        public void Write(byte value) => WriteNumber(value);

        public void Write(ushort value) => WriteNumber(value);

        public void Write(uint value) => WriteNumber(value);

        public void Write(ulong value) => WriteNumber(value);

        public void Write(char c)
        {
            EnsureCapacity(sizeof(char));
            *(char*)_currentPointer = c;
            _currentPointer += sizeof(char);
        }

        public void Write(string value)
        {
            fixed (char* c = value)
            {
                Write(c, value.Length);
            }
        }

        public void Write(ReadOnlyMemory<char> value) => Write(value.Span);

        public void Write(ReadOnlySpan<char> value)
        {
            fixed (char* c = value)
            {
                Write(c, value.Length);
            }
        }

        public void Write(char* ptr, int length)
        {
            int byteLength = length * sizeof(char);
            EnsureCapacity(byteLength);
            Unsafe.CopyBlock(_currentPointer, ptr, (uint)byteLength);
            _currentPointer += byteLength;
        }

        public void EnsureCapacity(int amount)
        {
            if (amount <= RemainingBytes)
            {
                return;
            }

            var oldBlock = _block;
            var offset = Offset;
            long newLength = Math.Max(_block.Length + amount, _block.Length * 2);
            _block = new Block((int)newLength);
            _currentPointer = _block.StartPointer + offset;
            if (oldBlock.Length > 0)
            {
                Unsafe.CopyBlock(_block.StartPointer, oldBlock.StartPointer, (uint)offset);
            }

            oldBlock.Dispose();
        }

        public override string ToString()
        {
            if (_block.Length == 0)
            {
                return string.Empty;
            }

            return new string((char*)_block.StartPointer, 0, (int)Offset);
        }

        public ReadOnlySpan<char> AsSpan()
        {
            return new ReadOnlySpan<char>(_block.StartPointer, (int)Offset);
        }

        public char* UnsafeAsPointer(out int length)
        {
            length = (int)Offset;
            return (char*)_block.StartPointer;
        }

        public void Clear()
        {
            Unsafe.InitBlock(_block.StartPointer, 0, (uint)RemainingBytes);
            _currentPointer = _block.StartPointer;
        }

        private void WriteNumber(long value)
        {
            if (value < 0)
            {
                Write('-');
                WriteNumber((ulong)Math.Abs(value));
                return;
            }

            WriteNumber((ulong)value);
        }

        private void WriteNumber(ulong value)
        {
            int digitCount = FormattingHelpers.CountDigits(value);
            int byteCount = digitCount * sizeof(char);
            EnsureCapacity(byteCount);
            _currentPointer += byteCount;
            byte* ptr = _currentPointer;

            for (int i = digitCount; i >= 1; i--, ptr -= 2)
            {
                ulong temp = '0' + value;
                value /= 10;
                *ptr = (byte)(temp - (value * 10));
                ptr[1] = 0;
            }

            *(--ptr) = 0;
            *(--ptr) = (byte)('0' + value);
            _currentPointer += byteCount;
        }
    }
}
