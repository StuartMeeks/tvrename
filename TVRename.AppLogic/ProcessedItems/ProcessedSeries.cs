using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.Settings;
using TVRename.AppLogic.TheTvDb;

namespace TVRename.AppLogic.ProcessedItems
{
    public class ProcessedSeries
    {
        public bool AutoAddNewSeasons;
        public string AutoAdd_FolderBase; // TODO: use magical renaming tokens here
        public bool AutoAdd_FolderPerSeason;
        public string AutoAdd_SeasonFolderName; // TODO: use magical renaming tokens here

        public bool CountSpecials;
        public string CustomShowName;
        public bool DVDOrder; // sort by DVD order, not the default sort we get
        public bool DoMissingCheck;
        public bool DoRename;
        public bool ForceCheckFuture;
        public bool ForceCheckNoAirdate;
        public List<int> IgnoreSeasons;
        public Dictionary<int, List<String>> ManualFolderLocations;
        public bool PadSeasonToTwoDigits;
        public Dictionary<int, List<ProcessedEpisode>> SeasonEpisodes; // built up by applying rules.
        public Dictionary<int, List<SeasonRule>> SeasonRules;
        public bool ShowNextAirdate;
        public int TVDBCode;
        public bool UseCustomShowName;
        public bool UseSequentialMatch;
        public List<string> AliasNames = new List<string>();
        public bool UseCustomSearchURL;
        public String CustomSearchURL;

        public String ShowTimeZone;
        private TimeZoneInfo SeriesTZ;
        private string LastFiguredTZ;


        public DateTime? BannersLastUpdatedOnDisk { get; set; }

        public ProcessedSeries()
        {
            SetDefaults();
        }

        public ProcessedSeries(int tvDBCode)
        {
            SetDefaults();
            this.TVDBCode = tvDBCode;
        }

        private void FigureOutTimeZone()
        {
            string tzstr = this.ShowTimeZone;

            if (string.IsNullOrEmpty(tzstr))
                tzstr = TimeZoneHelper.DefaultTimeZone();

            this.SeriesTZ = TimeZoneHelper.TimeZoneFor(tzstr);

            this.LastFiguredTZ = tzstr;
        }

        public TimeZoneInfo GetTimeZone()
        {
            // we cache the timezone info, as the fetching is a bit slow, and we do this a lot
            if (this.LastFiguredTZ != this.ShowTimeZone)
                this.FigureOutTimeZone();

            return this.SeriesTZ;
        }

        public ProcessedSeries(XmlReader reader)
        {
            SetDefaults();

            reader.Read();
            if (reader.Name != "ShowItem")
                return; // bail out

            reader.Read();
            while (!reader.EOF)
            {
                if ((reader.Name == "ShowItem") && !reader.IsStartElement())
                    break; // all done

                if (reader.Name == "ShowName")
                {
                    this.CustomShowName = reader.ReadElementContentAsString();
                    this.UseCustomShowName = true;
                }
                if (reader.Name == "UseCustomShowName")
                    this.UseCustomShowName = reader.ReadElementContentAsBoolean();
                if (reader.Name == "CustomShowName")
                    this.CustomShowName = reader.ReadElementContentAsString();
                else if (reader.Name == "TVDBID")
                    this.TVDBCode = reader.ReadElementContentAsInt();
                else if (reader.Name == "CountSpecials")
                    this.CountSpecials = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ShowNextAirdate")
                    this.ShowNextAirdate = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "AutoAddNewSeasons")
                    this.AutoAddNewSeasons = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "FolderBase")
                    this.AutoAdd_FolderBase = reader.ReadElementContentAsString();
                else if (reader.Name == "FolderPerSeason")
                    this.AutoAdd_FolderPerSeason = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "SeasonFolderName")
                    this.AutoAdd_SeasonFolderName = reader.ReadElementContentAsString();
                else if (reader.Name == "DoRename")
                    this.DoRename = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "DoMissingCheck")
                    this.DoMissingCheck = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "DVDOrder")
                    this.DVDOrder = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "UseCustomSearchURL")
                    this.UseCustomSearchURL = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "CustomSearchURL")
                    this.CustomSearchURL = reader.ReadElementContentAsString();
                else if (reader.Name == "TimeZone")
                    this.ShowTimeZone = reader.ReadElementContentAsString();
                else if (reader.Name == "ForceCheckAll") // removed 2.2.0b2
                    this.ForceCheckNoAirdate = this.ForceCheckFuture = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ForceCheckFuture")
                    this.ForceCheckFuture = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ForceCheckNoAirdate")
                    this.ForceCheckNoAirdate = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "PadSeasonToTwoDigits")
                    this.PadSeasonToTwoDigits = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "BannersLastUpdatedOnDisk")
                {
                    if (!reader.IsEmptyElement)
                    {

                        this.BannersLastUpdatedOnDisk = reader.ReadElementContentAsDateTime();
                    }
                    else
                        reader.Read();
                }

                else if (reader.Name == "UseSequentialMatch")
                    this.UseSequentialMatch = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "IgnoreSeasons")
                {
                    if (!reader.IsEmptyElement)
                    {
                        reader.Read();
                        while (reader.Name != "IgnoreSeasons")
                        {
                            if (reader.Name == "Ignore")
                                this.IgnoreSeasons.Add(reader.ReadElementContentAsInt());
                            else
                                reader.ReadOuterXml();
                        }
                    }
                    reader.Read();
                }
                else if (reader.Name == "AliasNames")
                {
                    if (!reader.IsEmptyElement)
                    {
                        reader.Read();
                        while (reader.Name != "AliasNames")
                        {
                            if (reader.Name == "Alias")
                                this.AliasNames.Add(reader.ReadElementContentAsString());
                            else
                                reader.ReadOuterXml();
                        }
                    }
                    reader.Read();
                }
                else if (reader.Name == "Rules")
                {
                    if (!reader.IsEmptyElement)
                    {
                        int snum = int.Parse(reader.GetAttribute("SeasonNumber"));
                        this.SeasonRules[snum] = new List<SeasonRule>();
                        reader.Read();
                        while (reader.Name != "Rules")
                        {
                            if (reader.Name == "Rule")
                            {
                                this.SeasonRules[snum].Add(new SeasonRule(reader.ReadSubtree()));
                                reader.Read();
                            }
                        }
                    }
                    reader.Read();
                }
                else if (reader.Name == "SeasonFolders")
                {
                    if (!reader.IsEmptyElement)
                    {
                        int snum = int.Parse(reader.GetAttribute("SeasonNumber"));
                        this.ManualFolderLocations[snum] = new List<String>();
                        reader.Read();
                        while (reader.Name != "SeasonFolders")
                        {
                            if ((reader.Name == "Folder") && reader.IsStartElement())
                            {
                                string ff = reader.GetAttribute("Location");
                                if (AutoFolderNameForSeason(snum) != ff)
                                    this.ManualFolderLocations[snum].Add(ff);
                            }
                            reader.Read();
                        }
                    }
                    reader.Read();
                }

                else
                    reader.ReadOuterXml();
            } // while
        }

        internal bool UsesManualFolders()
        {
            return this.ManualFolderLocations.Count > 0;
        }

        public TheTvDbSeries TheSeries()
        {
            return TheTvDbClient.Instance.GetSeries(this.TVDBCode);
        }

        public string ShowName
        {
            get
            {
                if (this.UseCustomShowName)
                    return this.CustomShowName;
                TheTvDbSeries ser = TheSeries();
                if (ser != null)
                    return ser.Name;
                return "<" + this.TVDBCode + " not downloaded>";
            }
        }

        public List<String> getSimplifiedPossibleShowNames()
        {
            List<String> possibles = new List<String>();

            String simplifiedShowName = FileHelper.SimplifyName(this.ShowName);
            if (!(simplifiedShowName == "")) { possibles.Add(simplifiedShowName); }

            //Check the custom show name too
            if (this.UseCustomShowName)
            {
                String simplifiedCustomShowName = FileHelper.SimplifyName(this.CustomShowName);
                if (!(simplifiedCustomShowName == "")) { possibles.Add(simplifiedCustomShowName); }
            }

            //Also add the aliases provided
            possibles.AddRange(from alias in this.AliasNames select FileHelper.SimplifyName(alias));

            return possibles;

        }

        public string ShowStatus
        {
            get
            {
                TheTvDbSeries ser = TheSeries();
                if (ser != null) return ser.getStatus();
                return "Unknown";
            }
        }

        public enum ShowAirStatus
        {
            NoEpisodesOrSeasons,
            Aired,
            PartiallyAired,
            NoneAired
        }

        public ShowAirStatus SeasonsAirStatus
        {
            get
            {
                if (this.HasSeasonsAndEpisodes)
                {
                    if (this.HasAiredEpisodes && !this.HasUnairedEpisodes)
                    {
                        return ShowAirStatus.Aired;
                    }
                    else if (this.HasUnairedEpisodes && !this.HasAiredEpisodes)
                    {
                        return ShowAirStatus.NoneAired;
                    }
                    else if (this.HasAiredEpisodes && this.HasUnairedEpisodes)
                    {
                        return ShowAirStatus.PartiallyAired;
                    }
                    else
                    {
                        //System.Diagnostics.Debug.Assert(false, "That is weird ... we have 'seasons and episodes' but none are aired, nor unaired. That case shouldn't actually occur !");
                        return ShowAirStatus.NoEpisodesOrSeasons;
                    }
                }
                else
                {
                    return ShowAirStatus.NoEpisodesOrSeasons;
                }
            }
        }

        private bool HasSeasonsAndEpisodes
        {
            get
            {
                //We can use AiredSeasons as it does not matter which order we do this in Aired or DVD
                if (TheSeries() == null || TheSeries().AiredSeasons == null || TheSeries().AiredSeasons.Count <= 0)
                    return false;
                foreach (KeyValuePair<int, TheTvDbSeason> s in TheSeries().AiredSeasons)
                {
                    if (this.IgnoreSeasons.Contains(s.Key))
                        continue;
                    if (s.Value.Episodes != null && s.Value.Episodes.Count > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private bool HasUnairedEpisodes
        {
            get
            {
                if (!this.HasSeasonsAndEpisodes) return false;

                foreach (KeyValuePair<int, TheTvDbSeason> s in TheSeries().AiredSeasons)
                {
                    if (this.IgnoreSeasons.Contains(s.Key))
                        continue;
                    if (s.Value.Status(GetTimeZone()) == TheTvDbSeasonStatus.NoneAired ||
                        s.Value.Status(GetTimeZone()) == TheTvDbSeasonStatus.PartiallyAired)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private bool HasAiredEpisodes
        {
            get
            {
                if (!this.HasSeasonsAndEpisodes) return false;

                foreach (KeyValuePair<int, TheTvDbSeason> s in TheSeries().AiredSeasons)
                {
                    if (this.IgnoreSeasons.Contains(s.Key))
                        continue;
                    if (s.Value.Status(GetTimeZone()) == TheTvDbSeasonStatus.PartiallyAired || s.Value.Status(GetTimeZone()) == TheTvDbSeasonStatus.Aired)
                    {
                        return true;
                    }
                }
                return false;
            }
        }


        public string[] Genres => TheSeries()?.GetGenres();

        public void SetDefaults()
        {
            ManualFolderLocations = new Dictionary<int, List<string>>();
            IgnoreSeasons = new List<int>();
            UseCustomShowName = false;
            CustomShowName = "";
            UseSequentialMatch = false;
            SeasonRules = new Dictionary<int, List<SeasonRule>>();
            SeasonEpisodes = new Dictionary<int, List<ProcessedEpisode>>();
            ShowNextAirdate = true;
            TVDBCode = -1;
            AutoAddNewSeasons = true;
            PadSeasonToTwoDigits = false;
            AutoAdd_FolderBase = "";
            AutoAdd_FolderPerSeason = true;
            AutoAdd_SeasonFolderName = "Season ";
            DoRename = true;
            DoMissingCheck = true;
            CountSpecials = false;
            DVDOrder = false;
            CustomSearchURL = "";
            UseCustomSearchURL = false;
            ForceCheckNoAirdate = false;
            ForceCheckFuture = false;
            BannersLastUpdatedOnDisk = null; //assume that the baners are old and have expired
            ShowTimeZone = TimeZoneHelper.DefaultTimeZone(); // default, is correct for most shows
            LastFiguredTZ = "";
        }

        public List<SeasonRule> RulesForSeason(int n)
        {
            return this.SeasonRules.ContainsKey(n) ? this.SeasonRules[n] : null;
        }

        public string AutoFolderNameForSeason(int n)
        {
            bool leadingZero = ApplicationSettings.Instance.LeadingZeroOnSeason || PadSeasonToTwoDigits;
            string r = this.AutoAdd_FolderBase;
            if (string.IsNullOrEmpty(r))
                return "";

            if (!r.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                r += System.IO.Path.DirectorySeparatorChar.ToString();
            if (AutoAdd_FolderPerSeason)
            {
                if (n == 0)
                    r += ApplicationSettings.Instance.SpecialsFolderName;
                else
                {
                    r += AutoAdd_SeasonFolderName;
                    if ((n < 10) && leadingZero)
                    {
                        r += "0";
                    }
                    r += n.ToString();
                }
            }
            return r;
        }

        public int MaxSeason()
        {
            int max = 0;
            foreach (KeyValuePair<int, List<ProcessedEpisode>> kvp in this.SeasonEpisodes)
            {
                if (kvp.Key > max)
                    max = kvp.Key;
            }
            return max;
        }

        //StringNiceName(int season)
        //{
        //    // something like "Simpsons (S3)"
        //    return String.Concat(ShowName," (S",season,")");
        //}

        public void WriteXMLSettings(XmlWriter writer)
        {
            writer.WriteStartElement("ShowItem");

            XmlHelper.WriteElementToXML(writer, "UseCustomShowName", this.UseCustomShowName);
            XmlHelper.WriteElementToXML(writer, "CustomShowName", this.CustomShowName);
            XmlHelper.WriteElementToXML(writer, "ShowNextAirdate", this.ShowNextAirdate);
            XmlHelper.WriteElementToXML(writer, "TVDBID", this.TVDBCode);
            XmlHelper.WriteElementToXML(writer, "AutoAddNewSeasons", this.AutoAddNewSeasons);
            XmlHelper.WriteElementToXML(writer, "FolderBase", this.AutoAdd_FolderBase);
            XmlHelper.WriteElementToXML(writer, "FolderPerSeason", this.AutoAdd_FolderPerSeason);
            XmlHelper.WriteElementToXML(writer, "SeasonFolderName", this.AutoAdd_SeasonFolderName);
            XmlHelper.WriteElementToXML(writer, "DoRename", this.DoRename);
            XmlHelper.WriteElementToXML(writer, "DoMissingCheck", this.DoMissingCheck);
            XmlHelper.WriteElementToXML(writer, "CountSpecials", this.CountSpecials);
            XmlHelper.WriteElementToXML(writer, "DVDOrder", this.DVDOrder);
            XmlHelper.WriteElementToXML(writer, "ForceCheckNoAirdate", this.ForceCheckNoAirdate);
            XmlHelper.WriteElementToXML(writer, "ForceCheckFuture", this.ForceCheckFuture);
            XmlHelper.WriteElementToXML(writer, "UseSequentialMatch", this.UseSequentialMatch);
            XmlHelper.WriteElementToXML(writer, "PadSeasonToTwoDigits", this.PadSeasonToTwoDigits);
            XmlHelper.WriteElementToXML(writer, "BannersLastUpdatedOnDisk", this.BannersLastUpdatedOnDisk);
            XmlHelper.WriteElementToXML(writer, "TimeZone", this.ShowTimeZone);


            writer.WriteStartElement("IgnoreSeasons");
            foreach (int i in this.IgnoreSeasons)
            {
                XmlHelper.WriteElementToXML(writer, "Ignore", i);
            }
            writer.WriteEndElement();

            writer.WriteStartElement("AliasNames");
            foreach (string str in this.AliasNames)
            {
                XmlHelper.WriteElementToXML(writer, "Alias", str);
            }
            writer.WriteEndElement();

            XmlHelper.WriteElementToXML(writer, "UseCustomSearchURL", this.UseCustomSearchURL);
            XmlHelper.WriteElementToXML(writer, "CustomSearchURL", this.CustomSearchURL);

            foreach (KeyValuePair<int, List<SeasonRule>> kvp in this.SeasonRules)
            {
                if (kvp.Value.Count > 0)
                {
                    writer.WriteStartElement("Rules");
                    XmlHelper.WriteAttributeToXML(writer, "SeasonNumber", kvp.Key);

                    foreach (SeasonRule r in kvp.Value)
                        r.WriteXml(writer);

                    writer.WriteEndElement(); // Rules
                }
            }
            foreach (KeyValuePair<int, List<String>> kvp in this.ManualFolderLocations)
            {
                if (kvp.Value.Count > 0)
                {
                    writer.WriteStartElement("SeasonFolders");

                    XmlHelper.WriteAttributeToXML(writer, "SeasonNumber", kvp.Key);

                    foreach (string s in kvp.Value)
                    {
                        writer.WriteStartElement("Folder");
                        XmlHelper.WriteAttributeToXML(writer, "Location", s);
                        writer.WriteEndElement(); // Folder
                    }

                    writer.WriteEndElement(); // Rules
                }
            }

            writer.WriteEndElement(); // ShowItem
        }

        public static List<ProcessedEpisode> ProcessedListFromEpisodes(List<TheTvDbEpisode> el, ProcessedSeries si)
        {
            List<ProcessedEpisode> pel = new List<ProcessedEpisode>();
            foreach (TheTvDbEpisode e in el)
                pel.Add(new ProcessedEpisode(e, si));
            return pel;
        }

        public Dictionary<int, List<ProcessedEpisode>> GetDVDSeasons()
        {
            //We will create this on the fly
            Dictionary<int, List<ProcessedEpisode>> returnValue = new Dictionary<int, List<ProcessedEpisode>>();
            foreach (KeyValuePair<int, List<ProcessedEpisode>> kvp in this.SeasonEpisodes)
            {
                foreach (ProcessedEpisode ep in kvp.Value)
                {
                    if (string.IsNullOrWhiteSpace(ep.DVDSeason))
                        continue;

                    if (!int.TryParse(ep.DVDSeason, out int dvdSeasonId))
                        return null;
                    if (!returnValue.ContainsKey(dvdSeasonId))
                    {
                        returnValue.Add(dvdSeasonId, new List<ProcessedEpisode>());

                    }
                    returnValue[dvdSeasonId].Add(ep);
                }
            }

            return returnValue;
        }

        public Dictionary<int, List<string>> AllFolderLocations()
        {
            return AllFolderLocations(true);
        }

        public Dictionary<int, List<string>> AllFolderLocationsEpCheck(bool checkExist)
        {
            return AllFolderLocations(true, checkExist);
        }

        public Dictionary<int, List<string>> AllFolderLocations(bool manualToo, bool checkExist = true)
        {
            Dictionary<int, List<string>> fld = new Dictionary<int, List<string>>();

            if (manualToo)
            {
                foreach (KeyValuePair<int, List<string>> kvp in this.ManualFolderLocations)
                {
                    if (!fld.ContainsKey(kvp.Key))
                        fld[kvp.Key] = new List<String>();
                    foreach (string s in kvp.Value)
                        fld[kvp.Key].Add(s.TrimTrailingSlash());
                }
            }

            if (this.AutoAddNewSeasons && (!string.IsNullOrEmpty(this.AutoAdd_FolderBase)))
            {
                int highestThereIs = -1;
                foreach (KeyValuePair<int, List<ProcessedEpisode>> kvp in this.SeasonEpisodes)
                {
                    if (kvp.Key > highestThereIs)
                        highestThereIs = kvp.Key;
                }
                foreach (int i in this.SeasonEpisodes.Keys)
                {
                    if (this.IgnoreSeasons.Contains(i)) continue;

                    string newName = AutoFolderNameForSeason(i);
                    if (string.IsNullOrEmpty(newName)) continue;

                    if (checkExist && !Directory.Exists(newName)) continue;

                    if (!fld.ContainsKey(i)) fld[i] = new List<string>();

                    if (!fld[i].Contains(newName)) fld[i].Add(newName.TrimTrailingSlash());
                }
            }

            return fld;
        }

        public static int CompareShowItemNames(ProcessedSeries one, ProcessedSeries two)
        {
            string ones = one.ShowName; // + " " +one->SeasonNumber.ToString("D3");
            string twos = two.ShowName; // + " " +two->SeasonNumber.ToString("D3");
            return ones.CompareTo(twos);
        }

        public TheTvDbSeason GetSeason(int snum)
        {
            return this.DVDOrder ? TheSeries().DVDSeasons[snum] : TheSeries().AiredSeasons[snum];
        }
    }
}
