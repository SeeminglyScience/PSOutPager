using System.Collections.Immutable;

namespace PSOutPager
{
    internal static class Expect
    {
        public static Expectation Single(char value)
            => new Expectation(value);

        public static Expectation Either(char c0, char c1)
            => new Expectation(c0, c1);

        public static Expectation Any(char c0, char c1, char c2)
            => new Expectation(ImmutableArray.Create(c0, c1, c2));

        public static Expectation Any(char c0, char c1, char c2, char c3)
            => new Expectation(ImmutableArray.Create(c0, c1, c2, c3));

        public static Expectation Any(params char[] chars)
            => new Expectation(ImmutableArray.Create(chars));

        public static Expectation Any(ImmutableArray<char> many) => new Expectation(many);
    }

    internal readonly struct Expectation
    {
        public enum ExpectationKind
        {
            Single = 0,

            Either = 1,

            Any = 2,
        }

        public readonly ExpectationKind Kind;

        public readonly char Value;

        public readonly char OtherValue;

        public readonly ImmutableArray<char> Many;

        public Expectation(char value)
        {
            Kind = ExpectationKind.Single;
            Value = value;
            OtherValue = default;
            Many = default;
        }

        public Expectation(char value, char otherValue)
        {
            Kind = ExpectationKind.Either;
            Value = value;
            OtherValue = otherValue;
            Many = default;
        }

        public Expectation(ImmutableArray<char> many)
        {
            Kind = ExpectationKind.Any;
            Value = default;
            OtherValue = default;
            Many = many;
        }

        public static bool operator ==(Expectation w1, Expectation e2) => w1.Equals(e2);

        public static bool operator !=(Expectation e1, Expectation e2) => !e1.Equals(e2);

        public static implicit operator Expectation(char c) => new Expectation(c);

        public bool Equals(Expectation other)
        {
            return Kind == other.Kind
                && Value == other.Value
                && OtherValue == other.OtherValue
                && Many == other.Many;
        }

        public bool Match(Token t) => !t.IsDefault && Match(t.Value);

        public bool Match(char c)
        {
            if (Kind == ExpectationKind.Single)
            {
                return c == Value;
            }

            if (Kind == ExpectationKind.Either)
            {
                return c == Value || c == OtherValue;
            }

            return Many.IndexOf(c) != -1;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + Kind.GetHashCode();
                hash = (hash * 23) + Value.GetHashCode();
                hash = (hash * 23) + OtherValue.GetHashCode();
                hash = (hash * 23) + Many.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) => obj is Expectation other && Equals(other);
    }
}
