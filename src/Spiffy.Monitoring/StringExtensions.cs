using System.Text.RegularExpressions;

namespace Spiffy.Monitoring
{
    internal static class StringExtensions
    {
        static readonly Regex WhiteSpaceRegex =
            new Regex(@"\s+", RegexOptions.Compiled);

        public static bool ContainsWhiteSpace(this string value)
        {
            if (value == null) return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static string RemoveWhiteSpace(this string value)
        {
            return value == null ? null : WhiteSpaceRegex.Replace(value.Trim(), "_");
        }

        private static readonly ulong EncapsulationLookup = BuildLookup(' ', '"', '\'', ',', '&', '=');

        private static ulong BuildLookup(params char[] chars)
        {
            ulong mask = 0;
            foreach (var c in chars)
            {
                if (c < 64)
                    mask |= 1UL << c;
            }
            return mask;
        }

        private static bool NeedsEncapsulation(char c)
        {
            return c < 64 && (EncapsulationLookup & (1UL << c)) != 0;
        }

        public static bool RequiresEncapsulation(this string value, out char preferredQuote)
        {
            bool requiresEncapsulation = false;
            bool hasDouble = false, hasSingle = false, hasBacktick = false;

            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (NeedsEncapsulation(c))
                    requiresEncapsulation = true;
                if (c == '"') hasDouble = true;
                else if (c == '\'') hasSingle = true;
                else if (c == '`') hasBacktick = true;
            }

            preferredQuote = !hasDouble ? '"' : !hasSingle ? '\'' : !hasBacktick ? '`' : '"';
            return requiresEncapsulation;
        }

        private static readonly string DoubleQuote = "\"";
        private static readonly string SingleQuote = "'";
        private static readonly string Backtick = "`";

        public static string WrappedInQuotes(this string value, char quoteCharacter)
        {
            var q = quoteCharacter == '"' ? DoubleQuote
                  : quoteCharacter == '\'' ? SingleQuote
                  : quoteCharacter == '`' ? Backtick
                  : quoteCharacter.ToString();
            return string.Concat(q, value, q);
        }

        public static string WrappedInBrackets(this string value)
        {
            return string.Concat("[", value, "]");
        }
    }
}
