using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Alphaleonis.Win32.Filesystem;
using TVRename.AppLogic.Extensions;
using TVRename.AppLogic.Settings;

namespace TVRename.AppLogic.Helpers
{
    public static class FileHelper
    {
        public static bool IsSameDirectoryLocation(this string directoryPath1, string directoryPath2)
        {
            // http://stackoverflow.com/questions/1794025/how-to-check-whether-2-directoryinfo-objects-are-pointing-to-the-same-directory
            return string.Compare(directoryPath1.NormalizePath().TrimEnd('\\'), directoryPath2.NormalizePath().TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        public static string NormalizePath(this string path)
        {
            //https://stackoverflow.com/questions/2281531/how-can-i-compare-directory-paths-in-c
            return Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToUpperInvariant();
        }

        public static string SimplifyName(string n)
        {
            n = n.ToLower();
            n = n.Replace("the", string.Empty);
            n = n.Replace("'", string.Empty);
            n = n.Replace("&", string.Empty);
            n = n.Replace("and", string.Empty);
            n = n.Replace("!", string.Empty);
            n = Regex.Replace(n, "[_\\W]+", " ");

            return n.Trim();
        }

        public static bool IsSameFile(FileInfo a, FileInfo b)
        {
            return string.Compare(a.FullName, b.FullName, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static bool IsSameDirectory(DirectoryInfo a, DirectoryInfo b)
        {
            string n1 = a.FullName;
            string n2 = b.FullName;
            if (!n1.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                n1 = n1 + Path.DirectorySeparatorChar;
            }

            if (!n2.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                n2 = n2 + Path.DirectorySeparatorChar;
            }

            return string.Compare(n1, n2, StringComparison.CurrentCultureIgnoreCase) == 0;
        }

        public static string TrimTrailingSlash(this string s) // trim trailing slash
        {
            return s.TrimEnd(Path.DirectorySeparatorChar);
        }

        public static string CompareName(string n)
        {
            n = FileHelper.RemoveDiacritics(n);
            n = Regex.Replace(n, "[^\\w ]", "");
            return SimplifyName(n);

        }
        public static string RemoveExtension(this FileInfo file, bool useFullPath = false)
        {
            string root = useFullPath ? file.FullName : file.Name;

            return root.Substring(0, root.Length - file.Extension.Length);
        }

        public static string RemoveDiacritics(string stIn)
        {
            // From http://blogs.msdn.com/b/michkap/archive/2007/05/14/2629747.aspx
            string stFormD = stIn.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            for (int ich = 0; ich < stFormD.Length; ich++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(stFormD[ich]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(stFormD[ich]);
                }
            }
            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }

        public static FileInfo FileInFolder(string dir, string fn)
        {
            return new FileInfo(string.Concat(dir, dir.EndsWith(Path.DirectorySeparatorChar.ToString()) ? "" : Path.DirectorySeparatorChar.ToString(), fn));
        }

        public static FileInfo FileInFolder(DirectoryInfo di, string fn)
        {
            return FileInFolder(di.FullName, fn);
        }

        public static bool SimplifyAndCheckFilename(string filename, string showname, bool simplifyfilename = true, bool simplifyshowname = true)
        {
            return Regex.Match(simplifyfilename ? SimplifyName(filename) : filename, "\\b" + (simplifyshowname ? SimplifyName(showname) : showname) + "\\b", RegexOptions.IgnoreCase).Success;
        }

        public static bool IgnoreFile(FileInfo fi)
        {
            if (!ApplicationSettings.Instance.UsefulExtension(fi.Extension, false))
                return true; // move on

            if (ApplicationSettings.Instance.IgnoreSamples
                && fi.FullName.Contains("sample", StringComparison.OrdinalIgnoreCase)
                && fi.Length / (1024 * 1024) < ApplicationSettings.Instance.SampleFileMaxSizeMB)
            {
                return true;
            }

            if (fi.Name.StartsWith("-.") && fi.Length / 1024 < 10)
            {
                return true;
            }

            return false;
        }

    }
}
