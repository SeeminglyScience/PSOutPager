using System;

namespace PSOutPager
{
    public class ConsoleDisplayWriter : DisplayWriter
    {
        private protected override unsafe void InternalWrite(ReadOnlySpan<char> buffer)
        {
            fixed (char* c = buffer)
            {
                PlatformWindows.WriteOut(c, buffer.Length);
            }
        }
    }
}
