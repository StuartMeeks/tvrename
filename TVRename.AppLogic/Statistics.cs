using System;
using System.Xml;
using System.Xml.Serialization;
using Alphaleonis.Win32.Filesystem;

namespace TVRename.AppLogic
{
    [XmlRoot("Statistics", Namespace = "")]
    public class Statistics
    {
        public int AutoAddedShows = 0;
        public int FilesCopied = 0;
        public int FilesMoved = 0;
        public int FilesRenamed = 0;
        public int FindAndOrganisesDone = 0;
        public int MissingChecksDone = 0;
        public int RenameChecksDone = 0;
        public int TorrentsMatched = 0;

        // The following aren't saved, but are calculated when we do a scan
        [XmlIgnore] public int NumberOfEpisodes = -1; // -1 = unknown
        [XmlIgnore] public int NumberOfEpisodesExpected = 0;
        [XmlIgnore] public int NumberOfSeasons = 0;
        [XmlIgnore] public int NumberOfShows = 0;

        public static Statistics Load()
        {
            var statisticsFile = FileManager.StatisticsFile.FullName;
            return !File.Exists(statisticsFile)
                ? new Statistics()
                : LoadFrom(statisticsFile);
        }

        public void Save()
        {
            SaveToFile(FileManager.StatisticsFile.FullName);
        }

        private static Statistics LoadFrom(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;
            }

            XmlReaderSettings settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };
            Statistics sc;

            try
            {
                using (XmlReader reader = XmlReader.Create(filename, settings))
                {
                    XmlSerializer xs = new XmlSerializer(typeof(Statistics));
                    sc = (Statistics)xs.Deserialize(reader);
                    System.Diagnostics.Debug.Assert(sc != null);

                }
            }
            catch (Exception)
            {
                return new Statistics();
            }

            return sc;
        }

        private void SaveToFile(string toFile)
        {
            DirectoryInfo directory = new FileInfo(toFile).Directory;
            if (!directory.Exists)
            {
                directory.Create();
            }

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                NewLineOnAttributes = true
            };

            using (XmlWriter writer = XmlWriter.Create(toFile, settings))
            {
                XmlSerializer xs = new XmlSerializer(typeof(Statistics));
                xs.Serialize(writer, this);
            }
        }
    }
}

