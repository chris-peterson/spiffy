using System.Text.RegularExpressions;

namespace Spiffy.Monitoring
{
    internal static class StringExtensions
    {
        static readonly Regex WhiteSpaceRegex =
            new Regex(@"\s+", RegexOptions.Compiled);

        public static string RemoveWhiteSpace(this string value)
        {
            return value == null ? null : WhiteSpaceRegex.Replace(value.Trim(), "_");
        }

        static bool NeedsEncapsulation(char c)
        {
            switch (c)
            {
                case ' ':
                case '"':
                case '\'':
                case ',':
                case '&':
                case '=':
                    return true;
                default:
                    return false;
            }
        }

        public static bool RequiresEncapsulation(this string value, out char preferredQuote)
        {
            var requiresEncapsulation = false;
            bool hasDouble = false, hasSingle = false, hasBacktick = false;

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (NeedsEncapsulation(c))
                {
                    requiresEncapsulation = true;
                }
                if (c == '"')
                {
                    hasDouble = true;
                }
                else if (c == '\'')
                {
                    hasSingle = true;
                }
                else if (c == '`')
                {
                    hasBacktick = true;
                }
            }

            // Prefer a quote the value doesn't already contain, so it can't close early.
            if (!hasDouble)
            {
                preferredQuote = '"';
            }
            else if (!hasSingle)
            {
                preferredQuote = '\'';
            }
            else if (!hasBacktick)
            {
                preferredQuote = '`';
            }
            else
            {
                preferredQuote = '"';
            }

            return requiresEncapsulation;
        }

        public static string WrappedInQuotes(this string value, char quoteCharacter)
        {
            var quote = quoteCharacter.ToString();
            return string.Concat(quote, value, quote);
        }

        public static string WrappedInBrackets(this string value)
        {
            return string.Concat("[", value, "]");
        }
    }
}
