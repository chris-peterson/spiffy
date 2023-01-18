using System.Linq;
using System.Text.RegularExpressions;

namespace Spiffy.Monitoring
{
    internal static class StringExtensions
    {
        static readonly Regex WhiteSpaceRegex =
            new Regex(@"\s+", RegexOptions.Compiled);
        
        public static bool ContainsWhiteSpace(this string value)
        {
            return value != null && WhiteSpaceRegex.IsMatch(value);
        }

        public static string RemoveWhiteSpace(this string value)
        {
            return value == null ? null : WhiteSpaceRegex.Replace(value.Trim(), "_");
        }

        private static readonly char[] CharsThatRequiresEncapsulation = { ' ', '"', '\'', ',', '&' , '='};
        private static readonly char[] QuotePreference = { '"', '\'', '`' };
        public static bool RequiresEncapsulation(this string value, out char preferredQuote)
        {
            var requiresEncapsulation = false;
            var quoteIndex = 0;
            
            foreach (var c in value)
            {
                if (CharsThatRequiresEncapsulation.Contains(c))
                {
                    requiresEncapsulation = true;
                }
                if (quoteIndex < QuotePreference.Length && c == QuotePreference[quoteIndex])
                {
                    quoteIndex++;
                }
            }

            preferredQuote = quoteIndex >= QuotePreference.Length ? '"' : QuotePreference[quoteIndex];
            return requiresEncapsulation;
        }

        public static string WrappedInQuotes(this string value, char quoteCharacter)
        {
            return $"{quoteCharacter}{value}{quoteCharacter}";
        }

        public static string WrappedInBrackets(this string value)
        {
            return $"[{value}]";
        }
    }
}