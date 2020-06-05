using System.Runtime.CompilerServices;

namespace PSOutPager
{
    internal static class FormattingHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDigits(ulong value)
        {
            int digits = 1;
            uint part;
            if (value >= 10000000)
            {
                if (value >= 100000000000000)
                {
                    part = (uint)(value / 100000000000000);
                    digits += 14;
                }
                else
                {
                    part = (uint)(value / 10000000);
                    digits += 7;
                }
            }
            else
            {
                part = (uint)value;
            }

            if (part < 10)
            {
                return digits;
            }

            if (part < 100)
            {
                return digits + 1;
            }

            if (part < 1000)
            {
                return digits + 2;
            }

            if (part < 10000)
            {
                return digits + 3;
            }

            if (part < 100000)
            {
                return digits + 4;
            }

            if (part < 1000000)
            {
                return digits + 5;
            }

            return digits + 6;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDigits(uint value)
        {
            int digits = 1;
            if (value >= 100000)
            {
                value /= 100000;
                digits += 5;
            }

            if (value < 10)
            {
                return digits;
            }

            if (value < 100)
            {
                return digits + 1;
            }

            if (value < 1000)
            {
                return digits + 2;
            }

            if (value < 10000)
            {
                return digits + 3;
            }

            return digits + 4;
        }
    }
}
