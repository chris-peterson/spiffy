using System.Linq;
using System.Text.RegularExpressions;

namespace Spiffy.Monitoring
{
    internal static class StringExtensions
    {
        static readonly Regex WhiteSpaceRegex =
            new Regex(@"\s+", RegexOptions.Compiled);

        public static bool StartsWithQuote(this string value)
        {
            return string.IsNullOrEmpty(value) == false && value[0] == '"';
        }

        public static bool ContainsWhiteSpace(this string value)
        {
            return value != null && WhiteSpaceRegex.IsMatch(value);
        }

        public static string RemoveWhiteSpace(this string value)
        {
            return value == null ? null : WhiteSpaceRegex.Replace(value.Trim(), "_");
        }

        public static string WrappedInQuotes(this string value)
        {
            return $@"""{value}""";
        }

        public static string WrappedInBrackets(this string value)
        {
            return $"[{value}]";
        }

        public static bool IsNullOrWhiteSpace(this string value)
        {
            return value == null || value.All(char.IsWhiteSpace);
        }
    }
}