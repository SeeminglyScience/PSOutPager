using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PSOutPager
{
    internal static class PlatformWindows
    {
        private static readonly IntPtr s_outHandle;

        static PlatformWindows()
        {
            s_outHandle = Interop.GetStdHandle(Interop.STD_OUTPUT_HANDLE);
            if (s_outHandle == Interop.INVALID_HANDLE_VALUE)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public static unsafe void WriteOut(char* c, int length)
        {
            int result = Interop.WriteConsole(
                s_outHandle,
                c,
                length,
                lpNumberOfCharsWritten: out uint _,
                lpReserved: IntPtr.Zero);

            // Non-zero is success for WriteConsole.
            if (result == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private static class Interop
        {
            public const int STD_OUTPUT_HANDLE = -11;

            private const string Kernel32 = "kernel32";

            public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            [DllImport(Kernel32, SetLastError = true)]
            public static extern IntPtr GetStdHandle(int nStdHandle);

            [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern unsafe int WriteConsole(
                IntPtr hConsoleOutput,
                char* buffer,
                int nNumberOfCharsToWrite,
                out uint lpNumberOfCharsWritten,
                IntPtr lpReserved);
        }
    }
}
