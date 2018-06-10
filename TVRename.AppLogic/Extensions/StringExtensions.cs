using System;
using System.Collections.Generic;
using System.Linq;
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

        public static string TrimEnd(this string root, IEnumerable<string> endings)
        {
            return endings.Aggregate(root, (current, ending) => current.TrimEnd(ending));
        }

        public static string TrimEnd(this string root, string ending)
        {
            return !root.EndsWith(ending, StringComparison.OrdinalIgnoreCase)
                ? root
                : root.Substring(0, root.Length - ending.Length);
        }


    }
}
