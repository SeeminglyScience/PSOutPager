using System.Collections.Immutable;
using System.Diagnostics;

using static PSOutPager.Expect;

namespace PSOutPager
{
    internal unsafe ref struct AnsiEscapeParser
    {
        private static readonly ImmutableArray<char> s_ends = ImmutableArray.Create(
            new[]
            {
                'A', // Cursor Up by 1
                'B', // Cursor Down by 1
                'C', // Cursor Forward (Right) by 1
                'D', // Cursor Backword (Left) by 1
                'M', // Reverse Index
                '7', // Save Cursor Position in Memory
                '8', // Restore Cursor Position in Memory
                '=', // Enable Keypad Application Mode
                '>', // Enable Keypad Numeric Mode
                'H', // Horizontal Tab Set
            });

        private static readonly char[] s_openSquareNumber =
        {
            'A', // Cursor Up
            'B', // Cursor Down
            'C', // Cursor Forward
            'D', // Cursor Backward
            'E', // Cursor Next Line
            'F', // Cursor Previous Line
            'G', // Cursor Horizontal Absolute
            'd', // Vertical Line Position Absolute
            'S', // Scroll Up
            'T', // Scroll Down
            '@', // Insert Character
            'P', // Delete Character
            'X', // Erase Character
            'L', // Insert Line
            'M', // Delete Line
            'J', // Erase in Display
            'K', // Erase in Line
            'm', // Set Graphics Rendition
            'n', // Report Cursor Position
            'c', // Device Attributes
            'l', // Cursor Horizontal (Forward) Tab
            'Z', // Cursor Backwards Tab
        };

        private static readonly Expectation[] s_altScreenBuffer =
        {
            '1', '0', '4', '9', Either('h', 'l'),
        };

        private readonly char* End;

        private char* Current;

        public AnsiEscapeParser(char* buffer, char* end)
        {
            Current = buffer;
            End = end;
        }

        public static bool TryParseEscape(char* buffer, int length, out int charsProcessed)
        {
            var parser = new AnsiEscapeParser(buffer, buffer + length);
            bool wasSuccessful;
            try
            {
                wasSuccessful = parser.TryParseEscape();
            }
            catch (EndOfFileException)
            {
                charsProcessed = 0;
                return false;
            }

            if (!wasSuccessful)
            {
                charsProcessed = 0;
                return false;
            }

            charsProcessed = (int)(parser.Current - buffer) + 1;
            return true;
        }

        private bool TryParseEscape()
        {
            Debug.Assert(*Current == '\x1b', "caller should verify first char is an escape");

            if (NextIs(Any(s_ends)))
            {
                return true;
            }

            if (NextIs('['))
            {
                return TryParseOpenSquare();
            }

            if (NextIs(']'))
            {
                return TryParseCloseSquare();
            }

            return NextIs('(', Either('0', 'B'));
        }

        private bool TryParseCloseSquare()
        {
            if (NextIs('4'))
            {
                return NextIs(';')
                    && TrySkipNumbers()
                    && NextIs(';', 'r', 'g', 'b', ';')
                    && TrySkipNumbers()
                    && NextIs('/')
                    && TrySkipNumbers()
                    && NextIs('/')
                    && TrySkipNumbers()
                    && NextIs('\x1b');
            }

            if (!NextIs(Either('0', '2'), ';'))
            {
                return false;
            }

            char* original = Current;
            for (Current++; Current < End; Current++)
            {
                if (*Current == '\x07')
                {
                    return true;
                }
            }

            Current = original;
            return false;
        }

        private bool TryParseOpenSquare()
        {
            if (NextIs(Either('s', 'u')) ||
                NextIs('6', 'n') ||
                NextIs('0', Either('c', 'g')) ||
                NextIs('!', 'p'))
            {
                return true;
            }

            if (NextIs('?'))
            {
                if (NextIs(s_altScreenBuffer))
                {
                    return true;
                }

                if (NextIs(Either('1', '3'), Either('h', 'l')))
                {
                    return true;
                }

                if (NextIs('1', '2', Either('h', 'l')) || NextIs('2', '5', Either('h', 'l')))
                {
                    return true;
                }


            }

            if (NextIs(Either('3', '4'), '8', ';'))
            {
                if (NextIs('2'))
                {
                    return NextIs(';')
                        && TrySkipNumbers()
                        && NextIs(';')
                        && TrySkipNumbers()
                        && NextIs(';')
                        && TrySkipNumbers()
                        && NextIs('m');
                }

                if (NextIs('5'))
                {
                    return NextIs(';')
                        && TrySkipNumbers()
                        && NextIs('m');
                }
            }

            if (!TrySkipNumbers()) return false;

            // ESC [ <a> ; <b> .
            if (NextIs(';'))
            {
                if (!TrySkipNumbers()) return false;
                return NextIs(Any('H', 'f', 'r'));
            }

            return NextIs(Any(s_openSquareNumber));
        }

        private Token Get()
        {
            if (Current == End)
            {
                throw new EndOfFileException();
            }

            return new Token(*++Current);
        }

        private bool Unget()
        {
            Current--;
            return false;
        }

        private bool Unget(int count)
        {
            Current -= count;
            return false;
        }

        private bool NextIs(Expectation c0) => c0.Match(Get()) || Unget();

        private bool NextIs(Expectation c0, Expectation c1)
        {
            if ((End - Current) < 2)
            {
                return false;
            }

            if (!c0.Match(Get())) return Unget();
            if (!c1.Match(Get())) return Unget(count: 2);

            return true;
        }

        private bool NextIs(Expectation c0, Expectation c1, Expectation c2)
        {
            if ((End - Current) < 3)
            {
                return false;
            }

            if (!c0.Match(Get())) return Unget();
            if (!c1.Match(Get())) return Unget(count: 2);
            if (!c2.Match(Get())) return Unget(count: 3);

            return true;
        }

        private bool NextIs(
            Expectation c0,
            Expectation c1,
            Expectation c2,
            Expectation c3,
            Expectation c4)
        {
            if ((End - Current) < 5)
            {
                return false;
            }

            if (!c0.Match(Get())) return Unget();
            if (!c1.Match(Get())) return Unget(count: 2);
            if (!c2.Match(Get())) return Unget(count: 3);
            if (!c3.Match(Get())) return Unget(count: 4);
            if (!c4.Match(Get())) return Unget(count: 5);

            return true;
        }

        private bool NextIs(Expectation[] expectations)
        {
            for (int i = 0; i < expectations.Length; i++)
            {
                if (!expectations[i].Match(Get()))
                {
                    Unget(count: i + 1);
                    return false;
                }
            }

            return true;
        }

        private bool TrySkipNumbers()
        {
            bool seenDigits = false;
            char* original = Current;
            for (Current++; Current != End; Current++)
            {
                if (!char.IsDigit(*Current))
                {
                    Current--;
                    break;
                }

                seenDigits = true;
            }

            if (!seenDigits)
            {
                Current = original;
            }

            return seenDigits;
        }
    }
}
