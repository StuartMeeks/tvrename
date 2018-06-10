using System;
using System.Collections.Generic;
using System.Text;

namespace TVRename.AppLogic.Helpers
{
    public static class StringHelper
    {
        public static string GetCommonStartString(IEnumerable<string> testValues)
        {
            var root = string.Empty;
            var first = true;

            foreach (var test in testValues)
            {
                if (first)
                {
                    root = test;
                    first = false;
                }
                else
                {
                    root = GetCommonStartString(root, test);
                }

            }

            return root;
        }

        public static string GetCommonStartString(string first, string second)
        {
            var builder = new StringBuilder();
            var minLength = Math.Min(first.Length, second.Length);

            for (var i = 0; i < minLength; i++)
            {
                if (first[i].Equals(second[i]))
                {
                    builder.Append(first[i]);
                }
                else
                {
                    break;
                }
            }

            return builder.ToString();
        }

    }
}
