using System;
using System.IO;

namespace TVRename.AppLogic.Settings
{
    public static class FileSettings
    {
        private const string TvDbFileName = "TheTVDB.xml";
        private const string SettingsFileName = "TVRenameSettings.xml";
        private const string LayoutFileName = "TVRenameLayout.dat";
        private const string UiLayoutFileName = "Layout.xml";
        private const string StatisticsFileName = "Statistics.xml";

        private static string _userDefinedBasePath;

        public static FileInfo StatisticsFile => GetFileInfo(StatisticsFileName);
        public static FileInfo LayoutFile => GetFileInfo(LayoutFileName);
        public static FileInfo UiLayoutFile => GetFileInfo(UiLayoutFileName);
        public static FileInfo TvDbFile => GetFileInfo(TvDbFileName);
        public static FileInfo TvDocSettingsFile => GetFileInfo(SettingsFileName);

        public static void SetUserDefinedBasePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (File.Exists(path))
            {
                throw new ArgumentException(nameof(path));
            }
            path = Path.GetFullPath(path);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            _userDefinedBasePath = path;
        }

        private static FileInfo GetFileInfo(string file)
        {
            var path = !string.IsNullOrEmpty(_userDefinedBasePath)
                ? _userDefinedBasePath
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TVRename", "TVRename", "2.1");
            Directory.CreateDirectory(path);

            var filePath = Path.Combine(path, file);
            return new FileInfo(filePath);
        }
    }
}
