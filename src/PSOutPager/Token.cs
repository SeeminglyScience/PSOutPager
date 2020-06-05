using System;

namespace PSOutPager
{
    internal readonly struct Token : IEquatable<Token>
    {
        public readonly char Value;

        private readonly bool _isNotDefault;

        public Token(char value)
        {
            Value = value;
            _isNotDefault = true;
        }

        public bool IsDefault => !_isNotDefault;

        public static bool operator ==(Token first, Token second) => first.Equals(second);

        public static bool operator !=(Token first, Token second) => !first.Equals(second);

        public bool Equals(Token other) => _isNotDefault == other._isNotDefault && Value == other.Value;

        public override bool Equals(object obj) => obj is Token other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + Value.GetHashCode();
                hash = (hash * 23) + _isNotDefault.GetHashCode();
                return hash;
            }
        }
    }
}
