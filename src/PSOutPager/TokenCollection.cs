namespace PSOutPager
{
    internal unsafe ref struct TokenCollection
    {
        private readonly char** _ptr;

        private readonly int _amountRequested;

        private int _amountReturned;

        public TokenCollection(char** ptr, int amountRequested)
        {
            _ptr = ptr;
            _amountRequested = amountRequested;
            _amountReturned = 0;
        }

        public TokenCollection GetEnumerator() => this;

        public Token Current => _amountReturned > 0 ? new Token(**_ptr) : default;

        public bool MoveNext()
        {
            if (_amountReturned < _amountRequested)
            {
                _amountReturned++;
                (*_ptr)++;
                return true;
            }

            return false;
        }
    }
}
