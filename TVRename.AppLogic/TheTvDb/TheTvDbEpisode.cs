using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Newtonsoft.Json.Linq;
using TVRename.AppLogic.Helpers;

namespace TVRename.AppLogic.TheTvDb
{
    public class TheTvDbEpisode
    {
        public bool Dirty;
        public int AiredEpNum;
        public int DVDEpNum;
        public int EpisodeID;
        public DateTime? FirstAired;
        private Dictionary<string, string> Items; // other fields we don't specifically grab
        public string Overview;
        public string EpisodeRating;
        public string EpisodeGuestStars;
        public string EpisodeDirector;
        public string Writer;

        public int ReadAiredSeasonNum; // only use after loading to attach to the correct season!
        public int ReadDVDSeasonNum; // only use after loading to attach to the correct season!
        public int SeasonID;
        public int SeriesID;
        public long Srv_LastUpdated;

        public TheTvDbSeason TheAiredSeason;
        public TheTvDbSeason TheDVDSeason;
        public TheTvDbSeries TheSeries;
        private string mName;

        //TODO: Put this back
        // private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public TheTvDbEpisode(TheTvDbEpisode other)
        {
            EpisodeID = other.EpisodeID;
            SeriesID = other.SeriesID;
            AiredEpNum = other.AiredEpNum;
            DVDEpNum = other.DVDEpNum;
            FirstAired = other.FirstAired;
            Srv_LastUpdated = other.Srv_LastUpdated;
            Overview = other.Overview;
            EpisodeRating = other.EpisodeRating;
            EpisodeGuestStars = other.EpisodeGuestStars;
            EpisodeDirector = other.EpisodeDirector;
            Writer = other.Writer;
            Name = other.Name;
            TheAiredSeason = other.TheAiredSeason;
            TheDVDSeason = other.TheDVDSeason;
            TheSeries = other.TheSeries;
            SeasonID = other.SeasonID;
            Dirty = other.Dirty;

            Items = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> i in other.Items)
            {
                Items.Add(i.Key, i.Value);
            }
        }

        public TheTvDbEpisode(TheTvDbSeries series, TheTvDbSeason airSeason, TheTvDbSeason dvdSeason)
        {
            SetDefaults(series, airSeason, dvdSeason);
        }

        public DateTime? GetAirDateDT()
        {
            if (this.FirstAired == null)
                return null;

            DateTime fa = (DateTime)this.FirstAired;
            DateTime? airs = this.TheSeries.AirsTime;

            return new DateTime(fa.Year, fa.Month, fa.Day, airs?.Hour ?? 20, airs?.Minute ?? 0, 0, 0);
        }

        public DateTime? GetAirDateDT(TimeZoneInfo tz)
        {
            DateTime? dt = GetAirDateDT();
            if (dt == null) return null;

            return TimeZoneHelper.AdjustRemoteTimeToLocalTime(dt.Value, tz);
        }

        internal IEnumerable<KeyValuePair<string, string>> OtherItems()
        {

            List<string> skip = new List<String>
            {
                "id",
                "airedSeason",
                "airedSeasonID",
                "airedEpisodeNumber",
                "episodeName",
                "overview",
                "lastUpdated",
                "dvdSeason",
                "dvdEpisodeNumber",
                "dvdChapter",
                "absoluteNumber",
                "filename",
                "seriesId",
                "lastUpdatedBy",
                "airsAfterSeason",
                "airsBeforeSeason",
                "airsBeforeEpisode",
                "thumbAuthor",
                "thumbAdded",
                "thumbAdded",
                "thumbWidth",
                "thumbHeight",
                "director",
                "firstAired",
                "Combined_episodenumber",
                "Combined_season",
                "DVD_episodenumber",
                "DVD_season",
                "EpImgFlag",
                "absolute_number",
                "filename",
                "is_movie",
                "thumb_added",
                "thumb_height",
                "thumb_width",
                "EpisodeDirector"
            };
            return this.Items.Where(c => !skip.Contains(c.Key));

        }

        public string DVDEp => getValueAcrossVersions("dvdEpisodeNumber", "DVD_episodenumber", "");
        public string DVDSeason => getValueAcrossVersions("dvdSeason", "DVD_season", "");
        public string AirsBeforeSeason => getValueAcrossVersions("airsBeforeSeason", "airsbefore_season", "");
        public string AirsBeforeEpisode => getValueAcrossVersions("airsBeforeEpisode", "airsbefore_episode", "");

        public TheTvDbEpisode(TheTvDbSeries series, TheTvDbSeason seas, TheTvDbSeason dvdseas, XmlReader r) // TODO: Put this back?, CommandLineArgs args)
        {
            try
            {
                SetDefaults(series, seas, dvdseas);

                r.Read();
                if (r.Name != "Episode")
                    return;

                r.Read();
                while (!r.EOF)
                {
                    if ((r.Name == "Episode") && (!r.IsStartElement()))
                        break;
                    if (r.Name == "id")
                        this.EpisodeID = r.ReadElementContentAsInt();
                    else if (r.Name == "seriesid")
                        this.SeriesID = r.ReadElementContentAsInt(); // thetvdb series id
                    else if (r.Name == "seasonid")
                        this.SeasonID = r.ReadElementContentAsInt();
                    else if (r.Name == "EpisodeNumber")
                        this.AiredEpNum = r.ReadElementContentAsInt();
                    else if (r.Name == "dvdEpisodeNumber")
                    {
                        string den = r.ReadElementContentAsString();
                        int.TryParse(den, out this.DVDEpNum);
                    }
                    else if (r.Name == "SeasonNumber")
                    {
                        string sn = r.ReadElementContentAsString();
                        int.TryParse(sn, out this.ReadAiredSeasonNum);
                    }
                    else if (r.Name == "dvdSeason")
                    {
                        string dsn = r.ReadElementContentAsString();
                        int.TryParse(dsn, out this.ReadDVDSeasonNum);
                    }
                    else if (r.Name == "lastupdated")
                        this.Srv_LastUpdated = r.ReadElementContentAsLong();
                    else if (r.Name == "Overview")
                        this.Overview = XmlHelper.ReadStringFixQuotesAndSpaces(r);
                    else if (r.Name == "Rating")
                        this.EpisodeRating = XmlHelper.ReadStringFixQuotesAndSpaces(r);
                    else if (r.Name == "GuestStars")
                        this.EpisodeGuestStars = XmlHelper.ReadStringFixQuotesAndSpaces(r);
                    else if (r.Name == "EpisodeDirector")
                        this.EpisodeDirector = XmlHelper.ReadStringFixQuotesAndSpaces(r);
                    else if (r.Name == "Writer")
                        this.Writer = XmlHelper.ReadStringFixQuotesAndSpaces(r);
                    else if (r.Name == "EpisodeName")
                        this.Name = XmlHelper.ReadStringFixQuotesAndSpaces(r);
                    else if (r.Name == "FirstAired")
                    {
                        try
                        {
                            String contents = r.ReadElementContentAsString();
                            if (contents == "")
                            {
                                // TODO: Put this back
                                // logger.Info("Please confirm, but we are assuming that " + this.Name + "(episode Id =" + this.EpisodeID + ") has no airdate");
                                this.FirstAired = null;
                            }
                            else
                            {
                                this.FirstAired = DateTime.ParseExact(contents, "yyyy-MM-dd", new System.Globalization.CultureInfo(""));
                            }
                        }
                        catch
                        {
                            this.FirstAired = null;
                        }
                    }
                    else
                    {
                        if ((r.IsEmptyElement) || !r.IsStartElement())
                            r.ReadOuterXml();
                        else
                        {
                            XmlReader r2 = r.ReadSubtree();
                            r2.Read();
                            string name = r2.Name;
                            this.Items[name] = r2.ReadElementContentAsString();
                            r.Read();
                        }
                    }
                }
            }
            catch (XmlException e)
            {
                string message = "Error processing data from TheTVDB for an episode.";
                if (this.SeriesID != -1)
                    message += "\r\nSeries ID: " + this.SeriesID;
                if (this.EpisodeID != -1)
                    message += "\r\nEpisode ID: " + this.EpisodeID;
                if (this.DVDEpNum != -1)
                    message += "\r\nEpisode (DVD) Number: " + this.DVDEpNum;
                if (this.AiredEpNum != -1)
                    message += "\r\nEpisode Aired Number: " + this.AiredEpNum;
                if (!string.IsNullOrEmpty(this.Name))
                    message += "\r\nName: " + this.Name;

                // TODO: Put this back
                // logger.Error(e, message);

                throw new TheTvDbException(e.Message);
            }
        }

        public TheTvDbEpisode(int seriesId, JObject json, JObject jsonInDefaultLang)
        {
            SetDefaults(null, null, null);
            loadJSON(seriesId, json, jsonInDefaultLang);
        }

        public TheTvDbEpisode(int seriesId, JObject r)
        {
            SetDefaults(null, null, null);

            loadJSON(seriesId, r);
        }

        private void loadJSON(int seriesId, JObject bestLanguageR, JObject backupLanguageR)
        {
            //Here we have two pieces of JSON. One in local language and one in the default language (English). 
            //We will populate with the best language frst and then fillin any gaps with the backup Language
            loadJSON(seriesId, bestLanguageR);

            //backupLanguageR should be a series of name/value pairs (ie a JArray of JPropertes)
            //TVDB asserts that name and overview are the fields that are localised

            if ((string.IsNullOrWhiteSpace((string)bestLanguageR["episodeName"]) && ((string)backupLanguageR["episodeName"] != null)))
            {
                this.Name = (string)backupLanguageR["episodeName"];
                this.Items["episodeName"] = this.Name;
            }

            if ((string.IsNullOrWhiteSpace(this.Items["overview"]) && ((string)backupLanguageR["overview"] != null)))
            {
                this.Items["overview"] = (string)backupLanguageR["overview"];
                this.Overview = (string)backupLanguageR["overview"];
            }


        }

        private void loadJSON(int seriesId, JObject r)
        {
            //r should be a series of name/value pairs (ie a JArray of JPropertes)
            //save them all into the Items array for safe keeping
            foreach (JProperty episodeItems in r.Children<JProperty>())
            {
                try
                {
                    JToken currentData = (JToken)episodeItems.Value;
                    if (currentData.Type == JTokenType.Array) Items[episodeItems.Name] = JsonHelper.Flatten(currentData);
                    else if (currentData.Type != JTokenType.Object) //Ignore objects here as it is always the 'language' attribute that we do not need
                    {
                        JValue currentValue = (JValue)episodeItems.Value;
                        this.Items[episodeItems.Name] = currentValue.ToObject<string>();

                    }

                }
                catch (ArgumentException)
                {
                    // TODO: Put this back
                    // logger.Error("Could not parse Json for " + episodeItems.Name + " :" + ae.Message);
                    //ignore as probably a cast exception
                }
                catch (NullReferenceException)
                {
                    // TODO: Put this back
                    // logger.Error("Could not parse Json for " + episodeItems.Name + " :" + ae.Message);
                    //ignore as probably a cast exception
                }
                catch (InvalidCastException)
                {
                    // TODO: Put this back
                    // logger.Error("Could not parse Json for " + episodeItems.Name + " :" + ae.Message);
                    //ignore as probably a cast exception
                }
            }

            this.SeriesID = seriesId;
            try
            {
                this.EpisodeID = (int)r["id"];

                if ((string)r["airedSeasonID"] != null) { this.SeasonID = (int)r["airedSeasonID"]; }
                else
                {
                    // TODO: Put this back
                    // logger.Error("Issue with episode " + this.EpisodeID + " for series " + seriesId + " no airedSeasonID ");
                    // logger.Error(r.ToString());
                }

                this.AiredEpNum = (int)r["airedEpisodeNumber"];

                string dvdEpNumString = (string)r["dvdEpisodeNumber"];

                if (string.IsNullOrWhiteSpace(dvdEpNumString)) this.DVDEpNum = 0;
                else if (!int.TryParse(dvdEpNumString, out this.DVDEpNum)) this.DVDEpNum = 0;

                this.Srv_LastUpdated = (long)r["lastUpdated"];
                this.Overview = (string)r["overview"];
                this.EpisodeRating = (string)r["siteRating"];
                this.Name = (string)r["episodeName"];

                string sn = (string)r["airedSeason"];
                if (sn == null)
                {
                    // TODO: Put this back
                    // logger.Error("Issue with episode " + this.EpisodeID + " for series " + seriesId + " airedSeason = null");
                    // logger.Error(r.ToString());
                }
                else { int.TryParse(sn, out this.ReadAiredSeasonNum); }

                string dsn = (string)r["dvdSeason"];
                if (string.IsNullOrWhiteSpace(dsn)) this.ReadDVDSeasonNum = 0;
                else if (!int.TryParse(dsn, out this.ReadDVDSeasonNum)) this.ReadDVDSeasonNum = 0;

                this.EpisodeGuestStars = JsonHelper.Flatten(r["guestStars"], "|");
                this.EpisodeDirector = JsonHelper.Flatten(r["directors"], "|");
                this.Writer = JsonHelper.Flatten(r["writers"], "|");

                try
                {
                    string contents = (string)r["firstAired"];
                    if (string.IsNullOrEmpty(contents))
                    {
                        //if (this.ReadSeasonNum > 0)logger.Info("Please confirm, but we are assuming that " + this.Name + "(episode Id =" + this.EpisodeID + ") has no airdate");
                        this.FirstAired = null;
                    }
                    else
                    {
                        this.FirstAired = DateTime.ParseExact(contents, "yyyy-MM-dd", new System.Globalization.CultureInfo(""));
                    }
                }
                catch (Exception)
                {
                    // TODO: Put this back
                    // logger.Debug(e, "Failed to parse firstAired");
                    FirstAired = null;

                }
            }
            catch (Exception)
            {
                // TODO: Put this back
                // logger.Error(e, $"Failed to parse : {r.ToString() }");
            }
        }

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(this.mName))
                    return "Aired Episode " + this.AiredEpNum;
                return this.mName;
            }
            set => this.mName = value;
        }

        public int AiredSeasonNumber
        {
            get
            {
                if (this.TheAiredSeason != null)
                    return this.TheAiredSeason.SeasonNumber;
                return -1;
            }
        }

        public int DVDSeasonNumber
        {
            get
            {
                if (this.TheDVDSeason != null)
                    return this.TheDVDSeason.SeasonNumber;
                return -1;
            }
        }

        public bool SameAs(TheTvDbEpisode other)
        {
            return (this.EpisodeID == other.EpisodeID);
        }

        public string GetFilename() => getValueAcrossVersions("filename", "Filename", "");

        public string[] GetGuestStars()
        {

            string guest = this.EpisodeGuestStars;

            if (!string.IsNullOrEmpty(guest))
            {
                return guest.Split('|');

            }
            return new string[] { };

        }

        private string getValueAcrossVersions(string oldTag, string newTag, string defaultValue)
        {
            //Need to cater for new and old style tags (TVDB interface v1 vs v2)
            if (this.Items.ContainsKey(oldTag)) return this.Items[oldTag];
            if (this.Items.ContainsKey(newTag)) return this.Items[newTag];
            return defaultValue;
        }

        public bool OK()
        {
            bool returnVal = (this.SeriesID != -1) && (this.EpisodeID != -1) && (this.AiredEpNum != -1) && (this.SeasonID != -1) && (this.ReadAiredSeasonNum != -1);
            if (!returnVal)
            {
                // TODO: Put this back
                // logger.Warn("Issue with episode " + this.EpisodeID + " for series " + this.SeriesID + " for EpNum " + this.AiredEpNum + " for SeasonID " + this.SeasonID + " for ReadSeasonNum " + this.ReadAiredSeasonNum + " for DVDSeasonNum " + this.ReadDVDSeasonNum);
            }

            return returnVal;
        }

        public void SetDefaults(TheTvDbSeries series, TheTvDbSeason airSeason, TheTvDbSeason dvdSeason)
        {
            this.Items = new Dictionary<string, string>();
            this.TheAiredSeason = airSeason;
            this.TheDVDSeason = dvdSeason;

            this.TheSeries = series;

            this.Overview = "";
            this.EpisodeRating = "";
            this.EpisodeGuestStars = "";
            this.EpisodeDirector = "";
            this.Writer = "";
            this.Name = "";
            this.EpisodeID = -1;
            this.SeriesID = -1;
            this.ReadAiredSeasonNum = -1;
            this.ReadDVDSeasonNum = -1;
            this.AiredEpNum = -1;
            this.DVDEpNum = -1;
            this.FirstAired = null;
            this.Srv_LastUpdated = -1;
            this.Dirty = false;
        }

        public void SetSeriesSeason(TheTvDbSeries ser, TheTvDbSeason airedSeason, TheTvDbSeason dvdSeason)
        {
            this.TheAiredSeason = airedSeason;
            this.TheDVDSeason = dvdSeason;
            this.TheSeries = ser;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Episode");

            XmlHelper.WriteElementToXML(writer, "id", this.EpisodeID);
            XmlHelper.WriteElementToXML(writer, "seriesid", this.SeriesID);
            XmlHelper.WriteElementToXML(writer, "seasonid", this.SeasonID);
            XmlHelper.WriteElementToXML(writer, "EpisodeNumber", this.AiredEpNum);
            XmlHelper.WriteElementToXML(writer, "SeasonNumber", this.AiredSeasonNumber);
            XmlHelper.WriteElementToXML(writer, "dvdEpisodeNumber", this.DVDEpNum);
            XmlHelper.WriteElementToXML(writer, "dvdSeason", this.DVDSeasonNumber);
            XmlHelper.WriteElementToXML(writer, "lastupdated", this.Srv_LastUpdated);
            XmlHelper.WriteElementToXML(writer, "Overview", this.Overview);
            XmlHelper.WriteElementToXML(writer, "Rating", this.EpisodeRating);
            XmlHelper.WriteElementToXML(writer, "GuestStars", this.EpisodeGuestStars);
            XmlHelper.WriteElementToXML(writer, "EpisodeDirector", this.EpisodeDirector);
            XmlHelper.WriteElementToXML(writer, "Writer", this.Writer);
            XmlHelper.WriteElementToXML(writer, "EpisodeName", this.Name);

            if (this.FirstAired != null)
            {
                XmlHelper.WriteElementToXML(writer, "FirstAired", this.FirstAired.Value.ToString("yyyy-MM-dd"));
            }

            List<string> skip = new List<String>
                                  {
                                      "overview","Overview","seriesId","seriesid","lastupdated","lastUpdated","EpisodeName","episodeName","FirstAired","firstAired",
                                      "GuestStars","guestStars","director","directors","EpisodeDirector","Writer","Writers","id","seasonid","Overview","Rating"
                                  };

            foreach (KeyValuePair<string, string> kvp in this.Items)
            {
                if (!skip.Contains(kvp.Key))
                {
                    XmlHelper.WriteElementToXML(writer, kvp.Key, kvp.Value);
                }
            }



            writer.WriteEndElement(); //Episode
        }
    }
}
