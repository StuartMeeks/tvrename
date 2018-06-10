using System;
using System.IO;
using System.Xml;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems.Items;
using TVRename.AppLogic.Settings;
using TVRename.AppLogic.TheTvDb;

namespace TVRename.AppLogic.ScanItems.Actions
{

    public class NfoAction : ActionBase, WriteMetaDataAction
    {
        // Local Members
        public ProcessedSeries ItemSeries { get; } // if for an entire show, rather than specific episode
        public FileInfo TargetFile { get; }

        // Overrides for ItemBase
        public override string ItemTargetFolder => TargetFile?.DirectoryName;
        public override string ItemGroup => "lvgActionMeta";
        public override int ItemIconNumber => 7;
        public override IgnoreItem ItemIgnore => TargetFile == null ? null : new IgnoreItem(TargetFile.FullName);

        // Overrides for ActionBase
        public override string ActionName => "Write KODI Metadata";
        public override string ActionProgressText => TargetFile.Name;
        public override string ActionProduces => TargetFile.FullName;
        public override long ActionSizeOfWork => 10;

        // ctor
        public NfoAction(FileInfo file, ProcessedEpisode itemEpisode)
        {
            ItemSeries = null;
            ItemEpisode = itemEpisode;
            TargetFile = file;
        }

        public NfoAction(FileInfo file, ProcessedSeries itemSeries)
        {
            ItemSeries = itemSeries;
            ItemEpisode = null;
            TargetFile = file;
        }

        #region Item Members

        public override bool Equals(ItemBase other)
        {
            if (other is NfoAction realOther)
            {
                return FileHelper.IsSameFile(TargetFile, realOther.TargetFile);
            }

            return false;
        }

        public override int CompareTo(object other)
        {
            if (other == null)
            {
                return 1;
            }

            if (other.GetType() != typeof(NfoAction))
            {
                return 1;
            }

            return CompareTo(other as NfoAction);
        }

        public override int CompareTo(ItemBase other)
        {
            if (other is NfoAction realOther)
            {
                return string.Compare(TargetFile.FullName, realOther.TargetFile.FullName, StringComparison.Ordinal);
            }

            return 1;
        }

        #endregion


        private void WriteEpisodeDetailsFor(TheTvDbEpisode episode, XmlWriter writer, bool multi, bool dvdOrder)
        {
            // See: http://xbmc.org/wiki/?title=Import_-_Export_Library#TV_Episodes
            writer.WriteStartElement("episodedetails");

            XmlHelper.WriteElementToXML(writer, "title", episode.Name);
            XmlHelper.WriteElementToXML(writer, "showtitle", ItemEpisode.SI.ShowName);
            XmlHelper.WriteElementToXML(writer, "rating", episode.EpisodeRating);
            if (dvdOrder)
            {
                XmlHelper.WriteElementToXML(writer, "season", episode.DVDSeasonNumber);
                XmlHelper.WriteElementToXML(writer, "episode", episode.DVDEpNum);
            }
            else
            {
                XmlHelper.WriteElementToXML(writer, "season", episode.AiredSeasonNumber);
                XmlHelper.WriteElementToXML(writer, "episode", episode.AiredEpNum);

            }

            XmlHelper.WriteElementToXML(writer, "plot", episode.Overview);

            writer.WriteStartElement("aired");
            if (episode.FirstAired != null)
            {
                writer.WriteValue(episode.FirstAired.Value.ToString("yyyy-MM-dd"));
            }
            writer.WriteEndElement();

            XmlHelper.WriteElementToXML(writer, "mpaa", ItemEpisode.SI?.TheSeries()?.GetContentRating(), true);

            //Director(s)
            var epDirector = episode.EpisodeDirector;
            if (!string.IsNullOrEmpty(epDirector))
            {
                foreach (var daa in epDirector.Split('|'))
                {
                    XmlHelper.WriteElementToXML(writer, "director", daa, true);
                }
            }

            //Writers(s)
            var epWriter = episode.Writer;
            if (!string.IsNullOrEmpty(epWriter))
            {
                foreach (var txtWriter in epWriter.Split('|'))
                {
                    XmlHelper.WriteElementToXML(writer, "credits", txtWriter, true);
                }
            }

            // Guest Stars...
            if (!string.IsNullOrEmpty(episode.EpisodeGuestStars))
            {
                var recurringActors = "";

                if (ItemEpisode.SI != null)
                {
                    recurringActors = string.Join("|", ItemEpisode.SI.TheSeries().GetActors());
                }

                var guestActors = episode.EpisodeGuestStars;
                if (!string.IsNullOrEmpty(guestActors))
                {
                    foreach (var gaa in guestActors.Split('|'))
                    {
                        if (string.IsNullOrEmpty(gaa))
                        {
                            continue;
                        }

                        // Skip if the guest actor is also in the overal recurring list
                        if (!string.IsNullOrEmpty(recurringActors) && recurringActors.Contains(gaa))
                        {
                            continue;
                        }

                        writer.WriteStartElement("actor");
                        XmlHelper.WriteElementToXML(writer, "name", gaa);
                        writer.WriteEndElement(); // actor
                    }
                }
            }

            // actors...
            if (ItemEpisode.SI != null)
            {
                foreach (var aa in ItemEpisode.SI.TheSeries().GetActors())
                {
                    if (string.IsNullOrEmpty(aa))
                    {
                        continue;
                    }

                    writer.WriteStartElement("actor");
                    XmlHelper.WriteElementToXML(writer, "name", aa);
                    writer.WriteEndElement(); // actor
                }
            }

            if (multi)
            {
                writer.WriteStartElement("resume");
                //we have to put 0 as we don't know where the multipart episode starts/ends
                XmlHelper.WriteElementToXML(writer, "position", 0);
                XmlHelper.WriteElementToXML(writer, "total", 0);
                writer.WriteEndElement(); // resume

                //For now we only put art in for multipart episodes. Kodi finds the art appropriately
                //without our help for the others

                var episodeSi = ItemEpisode.SI ?? ItemSeries;
                var filename =
                    ApplicationSettings.Instance.FilenameFriendly(
                        ApplicationSettings.Instance.NamingStyle.GetTargetEpisodeName(episode, episodeSi.ShowName, episodeSi.GetTimeZone(), episodeSi.DVDOrder));

                var thumbFilename = filename + ".jpg";
                XmlHelper.WriteElementToXML(writer, "thumb", thumbFilename);
                //Should be able to do this using the local filename, but only seems to work if you provide a URL
                //XMLHelper.WriteElementToXML(writer, "thumb", TheTVDB.Instance.GetTVDBDownloadURL(episode.GetFilename()));
            }
            writer.WriteEndElement(); // episodedetails
        }

        public override bool PerformAction(ref bool pause, Statistics stats)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                NewLineOnAttributes = true,

                //Multipart NFO files are not actually valid XML as they have multiple episodeDetails elements
                ConformanceLevel = ConformanceLevel.Fragment
            };

            try
            {
                // "try" and silently fail.  eg. when file is use by other...
                using (var writer = XmlWriter.Create(TargetFile.FullName, settings))
                {
                    if (ItemEpisode != null) // specific episode
                    {
                        if (ItemEpisode.type == ProcessedEpisode.ProcessedEpisodeType.merged)
                        {
                            foreach (var ep in ItemEpisode.sourceEpisodes)
                            {
                                WriteEpisodeDetailsFor(ep, writer, true, ItemEpisode.SI.DVDOrder);
                            }
                        }
                        else
                        {
                            WriteEpisodeDetailsFor(ItemEpisode, writer, false, ItemEpisode.SI.DVDOrder);
                        }
                    }
                    else if (ItemSeries != null) // show overview (tvshow.nfo)
                    {
                        // http://www.xbmc.org/wiki/?title=Import_-_Export_Library#TV_Shows

                        writer.WriteStartElement("tvshow");

                        XmlHelper.WriteElementToXML(writer, "title", ItemSeries.ShowName);

                        XmlHelper.WriteElementToXML(writer, "episodeguideurl",
                            TheTvDbClient.BuildURL(true, true, ItemSeries.TVDBCode, TheTvDbClient.Instance.RequestLanguage));

                        XmlHelper.WriteElementToXML(writer, "plot", ItemSeries.TheSeries().GetOverview());

                        var genre = string.Join(" / ", ItemSeries.TheSeries().GetGenres());
                        if (!string.IsNullOrEmpty(genre))
                        {
                            XmlHelper.WriteElementToXML(writer, "genre", genre);
                        }

                        XmlHelper.WriteElementToXML(writer, "premiered", ItemSeries.TheSeries().GetFirstAired());
                        XmlHelper.WriteElementToXML(writer, "year", ItemSeries.TheSeries().GetYear());
                        XmlHelper.WriteElementToXML(writer, "rating", ItemSeries.TheSeries().GetContentRating());
                        XmlHelper.WriteElementToXML(writer, "status", ItemSeries.TheSeries().getStatus());

                        // actors...
                        foreach (var aa in ItemSeries.TheSeries().GetActors())
                        {
                            if (string.IsNullOrEmpty(aa))
                            {
                                continue;
                            }

                            writer.WriteStartElement("actor");
                            XmlHelper.WriteElementToXML(writer, "name", aa);
                            writer.WriteEndElement(); // actor
                        }

                        XmlHelper.WriteElementToXML(writer, "mpaa", ItemSeries.TheSeries().GetContentRating());
                        XmlHelper.WriteInfo(writer, "id", "moviedb", "imdb", ItemSeries.TheSeries().GetIMDB());

                        XmlHelper.WriteElementToXML(writer, "tvdbid", ItemSeries.TheSeries().TVDBCode);

                        var rt = ItemSeries.TheSeries().GetRuntime();
                        if (!string.IsNullOrEmpty(rt))
                        {
                            XmlHelper.WriteElementToXML(writer, "runtime", rt + " minutes");
                        }

                        writer.WriteEndElement(); // tvshow
                    }
                }
            }

            catch (Exception e)
            {
                ActionErrorText = e.Message;
                ActionError = true;
                ActionCompleted = true;
                return false;
            }

            ActionCompleted = true;
            return true;
        }

    }
}
