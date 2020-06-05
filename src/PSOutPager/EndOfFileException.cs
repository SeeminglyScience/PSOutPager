using System;

namespace PSOutPager
{
    internal sealed class EndOfFileException : Exception
    {
        public EndOfFileException()
        {
        }

        public EndOfFileException(string message)
            : base(message)
        {
        }

        public EndOfFileException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
