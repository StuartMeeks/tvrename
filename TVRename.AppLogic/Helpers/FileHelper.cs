using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
            var n1 = a.FullName;
            var n2 = b.FullName;
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

        public static bool IsSubfolderOf(this string thisOne, string ofThat)
        {
            // need terminating slash, otherwise "c:\abc def" will match "c:\abc"
            thisOne += Path.DirectorySeparatorChar.ToString();
            ofThat += Path.DirectorySeparatorChar.ToString();
            var l = ofThat.Length;

            return thisOne.Length >= l
                   && string.Equals(thisOne.Substring(0, l), ofThat, StringComparison.CurrentCultureIgnoreCase);
        }

        public static string TrimTrailingSlash(this string s) // trim trailing slash
        {
            return s.TrimEnd(Path.DirectorySeparatorChar);
        }

        public static string CompareName(string n)
        {
            n = RemoveDiacritics(n);
            n = Regex.Replace(n, "[^\\w ]", "");
            return SimplifyName(n);

        }
        public static string RemoveExtension(this FileInfo file, bool useFullPath = false)
        {
            var root = useFullPath ? file.FullName : file.Name;

            return root.Substring(0, root.Length - file.Extension.Length);
        }

        public static string RemoveDiacritics(string stIn)
        {
            // From http://blogs.msdn.com/b/michkap/archive/2007/05/14/2629747.aspx
            var stFormD = stIn.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var t in stFormD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(t);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(t);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        public static FileInfo FileInFolder(string dir, string fn)
        {
            return new FileInfo(string.Concat(dir,
                dir.EndsWith(Path.DirectorySeparatorChar.ToString())
                    ? ""
                    : Path.DirectorySeparatorChar.ToString(),
                fn));
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
            {
                return true; // move on
            }

            if (ApplicationSettings.Instance.IgnoreSamples
                && fi.FullName.Contains("sample", StringComparison.OrdinalIgnoreCase)
                && fi.Length / (1024 * 1024) < ApplicationSettings.Instance.SampleFileMaxSizeMB)
            {
                return true;
            }

            return fi.Name.StartsWith("-.") && fi.Length / 1024 < 10;
        }

        public static void Rotate(string filenameBase)
        {
            if (!File.Exists(filenameBase))
            {
                return;
            }

            for (var i = 8; i >= 0; i--)
            {
                var fn = filenameBase + "." + i;
                if (!File.Exists(fn))
                {
                    continue;
                }

                var fn2 = filenameBase + "." + (i + 1);
                if (File.Exists(fn2))
                {
                    File.Delete(fn2);
                }
                File.Move(fn, fn2);
            }

            File.Copy(filenameBase, filenameBase + ".0");
        }

        public static int GetFilmLength(this FileInfo movieFile)
        {
            string duration;
            using (ShellObject shell = ShellObject.FromParsingName(movieFile.FullName))
            {
                // alternatively: shell.Properties.GetProperty("System.Media.Duration");
                IShellProperty prop = shell.Properties.System.Media.Duration;
                // Duration will be formatted as 00:44:08
                duration = prop.FormatForDisplay(PropertyDescriptionFormatOptions.None);
            }

            return 3600 * int.Parse(duration.Split(':')[0]) + 60 * int.Parse(duration.Split(':')[1]) +
                   int.Parse(duration.Split(':')[2]);

        }

    }
}
