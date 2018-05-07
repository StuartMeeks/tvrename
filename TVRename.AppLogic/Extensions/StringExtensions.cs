using System;
using System.Text.RegularExpressions;

namespace TVRename.AppLogic.Extensions
{
    public static class StringExtensions
    {
        public static string ItemsPluralized(this int n)
        {
            return n == 1 ? "Item" : "Items";
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

        public static string ReplaceInsensitive(this string source, string search, string replacement)
        {
            return Regex.Replace(
                source,
                Regex.Escape(search),
                replacement.Replace("$", "$$"),
                RegexOptions.IgnoreCase
            );
        }
    }
}
