using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using TVRename.AppLogic.Settings;

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
            var statisticsFile = FileSettings.StatisticsFile.FullName;
            return !File.Exists(statisticsFile)
                ? new Statistics()
                : LoadFrom(statisticsFile);
        }

        public void Save()
        {
            SaveToFile(FileSettings.StatisticsFile.FullName);
        }

        private static Statistics LoadFrom(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;
            }

            var settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };
            Statistics sc;

            try
            {
                using (var reader = XmlReader.Create(filename, settings))
                {
                    var xs = new XmlSerializer(typeof(Statistics));
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
            var directory = new FileInfo(toFile).Directory;
            if (directory != null && !directory.Exists)
            {
                directory.Create();
            }

            var settings = new XmlWriterSettings
            {
                Indent = true,
                NewLineOnAttributes = true
            };

            using (var writer = XmlWriter.Create(toFile, settings))
            {
                var xs = new XmlSerializer(typeof(Statistics));
                xs.Serialize(writer, this);
            }
        }
    }
}

