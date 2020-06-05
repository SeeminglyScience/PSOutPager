using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PSOutPager
{
    internal readonly unsafe struct Block : IDisposable
    {
        public readonly byte* StartPointer;

        public readonly byte* EndPointer;

        public long Length => EndPointer - StartPointer;

        public bool IsDefault => new IntPtr(StartPointer) == IntPtr.Zero;

        public Block(int size)
        {
            StartPointer = (byte*)Marshal.AllocHGlobal(size);
            EndPointer = StartPointer + size;
            Unsafe.InitBlock(StartPointer, 0, (uint)size);
        }

        public void Dispose() => Marshal.FreeHGlobal(new IntPtr(StartPointer));
    }
}
