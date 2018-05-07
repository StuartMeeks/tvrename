using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Alphaleonis.Win32.Filesystem;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.TheTvDb;

namespace TVRename.AppLogic.Settings
{
    public sealed class ApplicationSettings
    {
        private static volatile ApplicationSettings _instance;
        private static readonly object syncRoot = new object();

        /// <summary>
        /// Singleton to access TVRename Application Settings
        /// </summary>
        public static ApplicationSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new ApplicationSettings();
                        }
                    }
                }

                return _instance;
            }
        }

        #region FolderJpgIsType enum

        public enum FolderJpgIsType
        {
            Banner,
            Poster,
            FanArt,
            SeasonPoster
        }
        #endregion

        #region WTWDoubleClickAction enum

        public enum WTWDoubleClickAction
        {
            Search,
            Scan
        }

        #endregion

        #region ScanType enum

        public enum ScanType
        {
            Full,
            Recent,
            Quick,
            SingleShow
        }

        #endregion

        public enum KODIType
        {
            Eden,
            Frodo,
            Both
        }

        public enum BetaMode
        {
            BetaToo,
            ProductionOnly
        }

        public enum KeepTogetherModes
        {
            All,
            AllBut,
            Just
        }

        public List<string> LibraryFoldersNames = new List<string>();
        public List<string> IgnoreFoldersNames = new List<string>();
        public List<string> DownloadFoldersNames = new List<string>();

        public bool AutoSelectShowInMyShows = true;
        public bool AutoCreateFolders = false;
        public bool BGDownload = false;
        public bool CheckuTorrent = false;
        public bool EpTBNs = false;
        public bool EpJPGs = false;
        public bool SeriesJpg = false;
        public bool ShrinkLargeMede8erImages = false;
        public bool FanArtJpg = false;
        public bool Mede8erXML = false;
        public bool ExportFOXML = false;
        public string ExportFOXMLTo = "";
        public bool ExportMissingCSV = false;
        public string ExportMissingCSVTo = "";
        public bool ExportMissingXML = false;
        public string ExportMissingXMLTo = "";
        public bool ExportShowsTXT = false;
        public string ExportShowsTXTTo = "";
        public int ExportRSSMaxDays = 7;
        public int ExportRSSMaxShows = 10;
        public int ExportRSSDaysPast = 0;
        public bool ExportRenamingXML = false;
        public string ExportRenamingXMLTo = "";
        public bool ExportWTWRSS = false;
        public string ExportWTWRSSTo = "";
        public bool ExportWTWXML = false;
        public string ExportWTWXMLTo = "";
        public List<FilenameProcessorRegEx> FNPRegexs = DefaultFNPList();
        public bool FolderJpg = false;
        public FolderJpgIsType FolderJpgIs = FolderJpgIsType.Poster;
        public ScanType MonitoredFoldersScanType = ScanType.Full;
        public KODIType SelectedKODIType = KODIType.Both;
        public bool ForceLowercaseFilenames = false;
        public bool IgnoreSamples = true;
        public bool KeepTogether = true;
        public bool LeadingZeroOnSeason = false;
        public bool LeaveOriginals = false;
        public bool LookForDateInFilename = false;
        public bool MissingCheck = true;
        public bool CorrectFileDates = false;
        public bool NFOShows = false;
        public bool NFOEpisodes = false;
        public bool KODIImages = false;
        public bool pyTivoMeta = false;
        public bool pyTivoMetaSubFolder = false;
        public CustomName NamingStyle = new CustomName();
        public bool NotificationAreaIcon = false;
        public bool OfflineMode = false;

        public BetaMode Mode = BetaMode.ProductionOnly;
        public float UpgradeDirtyPercent = 20;
        public KeepTogetherModes KeepTogetherMode = KeepTogetherModes.All;

        public bool BulkAddIgnoreRecycleBin = false;
        public bool BulkAddCompareNoVideoFolders = false;
        public string AutoAddMovieTerms = "dvdrip;camrip;screener;dvdscr;r5;bluray";
        public string AutoAddIgnoreSuffixes = "1080p;720p";

        public string[] AutoAddMovieTermsArray => this.AutoAddMovieTerms.Split(';');

        public string[] AutoAddIgnoreSuffixesArray => this.AutoAddIgnoreSuffixes.Split(';');

        public List<string> KeepTogetherExtensionsList => KeepTogetherExtensionsString.Split(';').ToList();
        public string KeepTogetherExtensionsString = "";

        public string defaultSeasonWord = "Season";

        public string[] searchSeasonWordsArray => this.searchSeasonWordsString.Split(';');

        public string searchSeasonWordsString = "Season;Series;Saison;Temporada;Seizoen";


        internal bool IncludeBetaUpdates()
        {
            return (this.Mode == BetaMode.BetaToo);
        }

        public string OtherExtensionsString = "";
        public SeriesFilter Filter = new SeriesFilter();

        public string[] OtherExtensionsArray => this.OtherExtensionsString.Split(';');

        public int ParallelDownloads = 4;
        public List<string> RSSURLs = DefaultRSSURLList();
        public bool RenameCheck = true;
        public bool PreventMove = false;
        public bool RenameTxtToSub = false;
        public List<Replacement> Replacements = DefaultListRE();
        public string ResumeDatPath = "";
        public int SampleFileMaxSizeMB = 50; // sample file must be smaller than this to be ignored
        public bool SearchLocally = true;
        public bool SearchRSS = false;
        public bool ShowEpisodePictures = true;
        public bool HideWtWSpoilers = false;
        public bool HideMyShowsSpoilers = false;
        public bool ShowInTaskbar = true;
        public bool AutoSearchForDownloadedFiles = false;
        public string SpecialsFolderName = "Specials";
        public int StartupTab = 0;
        public Searchers TheSearchers = new Searchers();

        public string[] VideoExtensionsArray => this.VideoExtensionsString.Split(';');


        public bool AutoMergeEpisodes = false;
        public string VideoExtensionsString = "";
        public int WTWRecentDays = 7;
        public string uTorrentPath = "";
        public bool MonitorFolders = false;
        public bool RemoveDownloadDirectoriesFiles = false;
        public ShowStatusColoringTypeList ShowStatusColors = new ShowStatusColoringTypeList();
        public string SABHostPort = "";
        public string SABAPIKey = "";
        public bool CheckSABnzbd = false;
        public string PreferredLanguage = "en";
        public WTWDoubleClickAction WTWDoubleClick;

        public TidySettings Tidyup = new TidySettings();
        public bool runPeriodicCheck = false;
        public int periodCheckHours = 1;
        public bool runStartupCheck = false;

        private ApplicationSettings()
        {
            SetToDefaults();
        }

        public void Load(XmlReader reader)
        {
            SetToDefaults();

            reader.Read();
            if (reader.Name != "Settings")
                return; // bail out

            reader.Read();
            while (!reader.EOF)
            {
                if ((reader.Name == "Settings") && !reader.IsStartElement())
                    break; // all done

                if (reader.Name == "Searcher")
                {
                    string srch = reader.ReadElementContentAsString(); // and match it based on name...
                    this.TheSearchers.CurrentSearch = srch;
                }
                else if (reader.Name == "TheSearchers")
                {
                    this.TheSearchers = new Searchers(reader.ReadSubtree());
                    reader.Read();
                }
                else if (reader.Name == "BGDownload")
                    this.BGDownload = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "OfflineMode")
                    this.OfflineMode = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "Replacements" && !reader.IsEmptyElement)
                {
                    this.Replacements.Clear();
                    reader.Read();
                    while (!reader.EOF)
                    {
                        if ((reader.Name == "Replacements") && (!reader.IsStartElement()))
                            break;
                        if (reader.Name == "Replace")
                        {
                            this.Replacements.Add(new Replacement(reader.GetAttribute("This"),
                                                                  reader.GetAttribute("That"),
                                                                  reader.GetAttribute("CaseInsensitive") == "Y"));
                            reader.Read();
                        }
                        else
                            reader.ReadOuterXml();
                    }
                    reader.Read();
                }
                else if (reader.Name == "ExportWTWRSS" && !reader.IsEmptyElement)
                    this.ExportWTWRSS = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportWTWRSSTo")
                    this.ExportWTWRSSTo = reader.ReadElementContentAsString();
                else if (reader.Name == "ExportWTWXML")
                    this.ExportWTWXML = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportWTWXMLTo")
                    this.ExportWTWXMLTo = reader.ReadElementContentAsString();
                else if (reader.Name == "WTWRecentDays")
                    this.WTWRecentDays = reader.ReadElementContentAsInt();
                else if (reader.Name == "StartupTab")
                {
                    int n = reader.ReadElementContentAsInt();
                    if (n == 6)
                        this.StartupTab = 2; // WTW is moved
                    else if ((n >= 1) && (n <= 3)) // any of the three scans
                        this.StartupTab = 1;
                    else
                        this.StartupTab = 0; // otherwise, My Shows
                }
                else if (reader.Name == "StartupTab2")
                    this.StartupTab = TabNumberFromName(reader.ReadElementContentAsString());
                else if (reader.Name == "DefaultNamingStyle") // old naming style
                    this.NamingStyle.StyleString = CustomName.OldNStyle(reader.ReadElementContentAsInt());
                else if (reader.Name == "NamingStyle")
                    this.NamingStyle.StyleString = reader.ReadElementContentAsString();
                else if (reader.Name == "NotificationAreaIcon")
                    this.NotificationAreaIcon = reader.ReadElementContentAsBoolean();
                else if ((reader.Name == "GoodExtensions") || (reader.Name == "VideoExtensions"))
                    this.VideoExtensionsString = reader.ReadElementContentAsString();
                else if (reader.Name == "OtherExtensions")
                    this.OtherExtensionsString = reader.ReadElementContentAsString();
                else if (reader.Name == "ExportRSSMaxDays")
                    this.ExportRSSMaxDays = reader.ReadElementContentAsInt();
                else if (reader.Name == "ExportRSSMaxShows")
                    this.ExportRSSMaxShows = reader.ReadElementContentAsInt();
                else if (reader.Name == "ExportRSSDaysPast")
                    this.ExportRSSDaysPast = reader.ReadElementContentAsInt();
                else if (reader.Name == "KeepTogether")
                    this.KeepTogether = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "LeadingZeroOnSeason")
                    this.LeadingZeroOnSeason = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ShowInTaskbar")
                    this.ShowInTaskbar = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "RenameTxtToSub")
                    this.RenameTxtToSub = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ShowEpisodePictures")
                    this.ShowEpisodePictures = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "HideWtWSpoilers")
                    this.HideWtWSpoilers = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "HideMyShowsSpoilers")
                    this.HideMyShowsSpoilers = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "AutoCreateFolders")
                    this.AutoCreateFolders = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "AutoSelectShowInMyShows")
                    this.AutoSelectShowInMyShows = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "SpecialsFolderName")
                    this.SpecialsFolderName = reader.ReadElementContentAsString();
                else if (reader.Name == "SABAPIKey")
                    this.SABAPIKey = reader.ReadElementContentAsString();
                else if (reader.Name == "CheckSABnzbd")
                    this.CheckSABnzbd = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "SABHostPort")
                    this.SABHostPort = reader.ReadElementContentAsString();
                else if (reader.Name == "PreferredLanguage")
                    this.PreferredLanguage = reader.ReadElementContentAsString();
                else if (reader.Name == "WTWDoubleClick")
                    this.WTWDoubleClick = (WTWDoubleClickAction)reader.ReadElementContentAsInt();
                else if (reader.Name == "ExportMissingXML")
                    this.ExportMissingXML = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportMissingXMLTo")
                    this.ExportMissingXMLTo = reader.ReadElementContentAsString();
                else if (reader.Name == "ExportMissingCSV")
                    this.ExportMissingCSV = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportMissingCSVTo")
                    this.ExportMissingCSVTo = reader.ReadElementContentAsString();
                else if (reader.Name == "ExportRenamingXML")
                    this.ExportRenamingXML = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportRenamingXMLTo")
                    this.ExportRenamingXMLTo = reader.ReadElementContentAsString();
                else if (reader.Name == "ExportFOXML")
                    this.ExportFOXML = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportFOXMLTo")
                    this.ExportFOXMLTo = reader.ReadElementContentAsString();
                else if (reader.Name == "ExportShowsTXT")
                    this.ExportShowsTXT = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportShowsTXTTo")
                    this.ExportShowsTXTTo = reader.ReadElementContentAsString();
                else if (reader.Name == "ForceLowercaseFilenames")
                    this.ForceLowercaseFilenames = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "IgnoreSamples")
                    this.IgnoreSamples = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "SampleFileMaxSizeMB")
                    this.SampleFileMaxSizeMB = reader.ReadElementContentAsInt();
                else if (reader.Name == "ParallelDownloads")
                    this.ParallelDownloads = reader.ReadElementContentAsInt();
                else if (reader.Name == "uTorrentPath")
                    this.uTorrentPath = reader.ReadElementContentAsString();
                else if (reader.Name == "ResumeDatPath")
                    this.ResumeDatPath = reader.ReadElementContentAsString();
                else if (reader.Name == "SearchRSS")
                    this.SearchRSS = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "EpImgs")
                    this.EpTBNs = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "NFOs") //support legacy tag
                {
                    this.NFOShows = reader.ReadElementContentAsBoolean();
                    this.NFOEpisodes = this.NFOShows;
                }
                else if (reader.Name == "NFOShows")
                    this.NFOShows = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "NFOEpisodes")
                    this.NFOEpisodes = reader.ReadElementContentAsBoolean();
                else if ((reader.Name == "XBMCImages") || (reader.Name == "KODIImages")) //Backward Compatibilty
                    this.KODIImages = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "pyTivoMeta")
                    this.pyTivoMeta = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "pyTivoMetaSubFolder")
                    this.pyTivoMetaSubFolder = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "FolderJpg")
                    this.FolderJpg = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "FolderJpgIs")
                    this.FolderJpgIs = (FolderJpgIsType)reader.ReadElementContentAsInt();
                else if (reader.Name == "MonitoredFoldersScanType")
                    this.MonitoredFoldersScanType = (ScanType)reader.ReadElementContentAsInt();
                else if ((reader.Name == "SelectedXBMCType") || (reader.Name == "SelectedKODIType"))
                    this.SelectedKODIType = (KODIType)reader.ReadElementContentAsInt();
                else if (reader.Name == "RenameCheck")
                    this.RenameCheck = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "PreventMove")
                    this.PreventMove = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "CheckuTorrent")
                    this.CheckuTorrent = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "MissingCheck")
                    this.MissingCheck = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "UpdateFileDates")
                    this.CorrectFileDates = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "SearchLocally")
                    this.SearchLocally = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "LeaveOriginals")
                    this.LeaveOriginals = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "AutoSearchForDownloadedFiles")
                    this.AutoSearchForDownloadedFiles = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "LookForDateInFilename")
                    this.LookForDateInFilename = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "AutoMergeEpisodes")
                    this.AutoMergeEpisodes = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "MonitorFolders")
                    this.MonitorFolders = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "StartupScan")
                    this.runStartupCheck = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "PeriodicScan")
                    this.runPeriodicCheck = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "PeriodicScanHours")
                    this.periodCheckHours = reader.ReadElementContentAsInt();
                else if (reader.Name == "RemoveDownloadDirectoriesFiles")
                    this.RemoveDownloadDirectoriesFiles = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "EpJPGs")
                    this.EpJPGs = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "SeriesJpg")
                    this.SeriesJpg = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "Mede8erXML")
                    this.Mede8erXML = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ShrinkLargeMede8erImages")
                    this.ShrinkLargeMede8erImages = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "FanArtJpg")
                    this.FanArtJpg = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "DeleteEmpty")
                    this.Tidyup.DeleteEmpty = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "DeleteEmptyIsRecycle")
                    this.Tidyup.DeleteEmptyIsRecycle = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "EmptyIgnoreWords")
                    this.Tidyup.EmptyIgnoreWords = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "EmptyIgnoreWordList")
                    this.Tidyup.EmptyIgnoreWordList = reader.ReadElementContentAsString();
                else if (reader.Name == "EmptyIgnoreExtensions")
                    this.Tidyup.EmptyIgnoreExtensions = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "EmptyIgnoreExtensionList")
                    this.Tidyup.EmptyIgnoreExtensionList = reader.ReadElementContentAsString();
                else if (reader.Name == "EmptyMaxSizeCheck")
                    this.Tidyup.EmptyMaxSizeCheck = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "EmptyMaxSizeMB")
                    this.Tidyup.EmptyMaxSizeMB = reader.ReadElementContentAsInt();

                else if (reader.Name == "BulkAddIgnoreRecycleBin")
                    this.BulkAddIgnoreRecycleBin = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "BulkAddCompareNoVideoFolders")
                    this.BulkAddCompareNoVideoFolders = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "AutoAddMovieTerms")
                    this.AutoAddMovieTerms = reader.ReadElementContentAsString();
                else if (reader.Name == "AutoAddIgnoreSuffixes")
                    this.AutoAddIgnoreSuffixes = reader.ReadElementContentAsString();

                else if (reader.Name == "BetaMode")
                    this.Mode = (BetaMode)reader.ReadElementContentAsInt();
                else if (reader.Name == "PercentDirtyUpgrade")
                    this.UpgradeDirtyPercent = reader.ReadElementContentAsFloat();
                else if (reader.Name == "BaseSeasonName")
                    this.defaultSeasonWord = reader.ReadElementContentAsString();
                else if (reader.Name == "SearchSeasonNames")
                    this.searchSeasonWordsString = reader.ReadElementContentAsString();
                else if (reader.Name == "KeepTogetherType")
                    this.KeepTogetherMode = (KeepTogetherModes)reader.ReadElementContentAsInt();
                else if (reader.Name == "KeepTogetherExtensions")
                    this.KeepTogetherExtensionsString = reader.ReadElementContentAsString();

                else if (reader.Name == "FNPRegexs" && !reader.IsEmptyElement)
                {
                    this.FNPRegexs.Clear();
                    reader.Read();
                    while (!reader.EOF)
                    {
                        if ((reader.Name == "FNPRegexs") && (!reader.IsStartElement()))
                            break;
                        if (reader.Name == "Regex")
                        {
                            string s = reader.GetAttribute("Enabled");
                            bool en = s == null || bool.Parse(s);

                            this.FNPRegexs.Add(new FilenameProcessorRegEx(reader.GetAttribute("RE"),
                                bool.Parse(reader.GetAttribute("UseFullPath")),
                                en,
                                reader.GetAttribute("Notes")));

                            reader.Read();
                        }
                        else
                            reader.ReadOuterXml();
                    }
                    reader.Read();
                }
                else if (reader.Name == "RSSURLs" && !reader.IsEmptyElement)
                {
                    this.RSSURLs.Clear();
                    reader.Read();
                    while (!reader.EOF)
                    {
                        if ((reader.Name == "RSSURLs") && (!reader.IsStartElement()))
                            break;
                        if (reader.Name == "URL")
                            this.RSSURLs.Add(reader.ReadElementContentAsString());
                        else
                            reader.ReadOuterXml();
                    }
                    reader.Read();
                }
                else if (reader.Name == "ShowStatusTVWColors" && !reader.IsEmptyElement)
                {
                    this.ShowStatusColors = new ShowStatusColoringTypeList();
                    reader.Read();
                    while (!reader.EOF)
                    {
                        if ((reader.Name == "ShowStatusTVWColors") && (!reader.IsStartElement()))
                            break;
                        if (reader.Name == "ShowStatusTVWColor")
                        {
                            ShowStatusColoringType type = null;
                            try
                            {
                                string showStatus = reader.GetAttribute("ShowStatus");
                                bool isMeta = bool.Parse(reader.GetAttribute("IsMeta"));
                                bool isShowLevel = bool.Parse(reader.GetAttribute("IsShowLevel"));

                                type = new ShowStatusColoringType(showStatus, isMeta, isShowLevel);
                            }
                            catch
                            {
                            }

                            string color = reader.GetAttribute("Color");
                            if (type != null && !string.IsNullOrEmpty(color))
                            {
                                try
                                {
                                    System.Drawing.Color c = System.Drawing.ColorTranslator.FromHtml(color);
                                    this.ShowStatusColors.Add(type, c);
                                }
                                catch
                                {
                                }
                            }
                            reader.Read();
                        }
                        else
                            reader.ReadOuterXml();
                    }
                    reader.Read();


                }
                else if (reader.Name == "ShowFilters" && !reader.IsEmptyElement)
                {
                    this.Filter = new SeriesFilter();
                    reader.Read();
                    while (!reader.EOF)
                    {
                        if ((reader.Name == "ShowFilters") && (!reader.IsStartElement()))
                            break;
                        if (reader.Name == "ShowNameFilter")
                        {
                            this.Filter.ShowName = reader.GetAttribute("ShowName");
                            reader.Read();
                        }
                        else if (reader.Name == "ShowStatusFilter")
                        {
                            this.Filter.ShowStatus = reader.GetAttribute("ShowStatus");
                            reader.Read();
                        }
                        else if (reader.Name == "ShowRatingFilter")
                        {
                            this.Filter.ShowRating = reader.GetAttribute("ShowRating");
                            reader.Read();
                        }
                        else if (reader.Name == "ShowNetworkFilter")
                        {
                            this.Filter.ShowNetwork = reader.GetAttribute("ShowNetwork");
                            reader.Read();
                        }
                        else if (reader.Name == "GenreFilter")
                        {
                            this.Filter.Genres.Add(reader.GetAttribute("Genre"));
                            reader.Read();
                        }
                        else
                            reader.ReadOuterXml();
                    }
                    reader.Read();
                }
                else
                    reader.ReadOuterXml();
            }
        }

        public void SetToDefaults()
        {
            // defaults that aren't handled with default initialisers

            this.VideoExtensionsString =
                ".avi;.mpg;.mpeg;.mkv;.mp4;.wmv;.divx;.ogm;.qt;.rm;.m4v;.webm;.vob;.ovg;.ogg;.mov;.m4p;.3gp";
            this.OtherExtensionsString = ".srt;.nfo;.txt;.tbn";
            this.KeepTogetherExtensionsString = ".srt;.nfo;.txt;.tbn";

            // have a guess at utorrent's path
            string[] guesses = new string[2];
            guesses[0] = "c:\\Program Files\\uTorrent\\uTorrent.exe";
            guesses[1] = "c:\\Program Files (x86)\\uTorrent\\uTorrent.exe";

            this.uTorrentPath = "";
            foreach (string g in guesses)
            {
                FileInfo f = new FileInfo(g);
                if (f.Exists)
                {
                    this.uTorrentPath = f.FullName;
                    break;
                }
            }

            // ResumeDatPath
            FileInfo f2 =
                new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"uTorrent\resume.dat"));
            this.ResumeDatPath = f2.Exists ? f2.FullName : "";
        }


        public void WriteXML(XmlWriter writer)
        {
            writer.WriteStartElement("Settings");
            this.TheSearchers.WriteXml(writer);
            XmlHelper.WriteElementToXML(writer, "BGDownload", this.BGDownload);
            XmlHelper.WriteElementToXML(writer, "OfflineMode", this.OfflineMode);
            writer.WriteStartElement("Replacements");
            foreach (Replacement R in this.Replacements)
            {
                writer.WriteStartElement("Replace");
                XmlHelper.WriteAttributeToXML(writer, "This", R.This);
                XmlHelper.WriteAttributeToXML(writer, "That", R.That);
                XmlHelper.WriteAttributeToXML(writer, "CaseInsensitive", R.IgnoreCase ? "Y" : "N");
                writer.WriteEndElement(); //Replace
            }
            writer.WriteEndElement(); //Replacements

            XmlHelper.WriteElementToXML(writer, "ExportWTWRSS", this.ExportWTWRSS);
            XmlHelper.WriteElementToXML(writer, "ExportWTWRSSTo", this.ExportWTWRSSTo);
            XmlHelper.WriteElementToXML(writer, "ExportWTWXML", this.ExportWTWXML);
            XmlHelper.WriteElementToXML(writer, "ExportWTWXMLTo", this.ExportWTWXMLTo);
            XmlHelper.WriteElementToXML(writer, "WTWRecentDays", this.WTWRecentDays);
            XmlHelper.WriteElementToXML(writer, "ExportMissingXML", this.ExportMissingXML);
            XmlHelper.WriteElementToXML(writer, "ExportMissingXMLTo", this.ExportMissingXMLTo);
            XmlHelper.WriteElementToXML(writer, "ExportMissingCSV", this.ExportMissingCSV);
            XmlHelper.WriteElementToXML(writer, "ExportMissingCSVTo", this.ExportMissingCSVTo);
            XmlHelper.WriteElementToXML(writer, "ExportRenamingXML", this.ExportRenamingXML);
            XmlHelper.WriteElementToXML(writer, "ExportRenamingXMLTo", this.ExportRenamingXMLTo);
            XmlHelper.WriteElementToXML(writer, "ExportShowsTXT", this.ExportShowsTXT);
            XmlHelper.WriteElementToXML(writer, "ExportShowsTXTTo", this.ExportShowsTXTTo);
            XmlHelper.WriteElementToXML(writer, "ExportFOXML", this.ExportFOXML);
            XmlHelper.WriteElementToXML(writer, "ExportFOXMLTo", this.ExportFOXMLTo);
            XmlHelper.WriteElementToXML(writer, "StartupTab2", TabNameForNumber(this.StartupTab));
            XmlHelper.WriteElementToXML(writer, "NamingStyle", this.NamingStyle.StyleString);
            XmlHelper.WriteElementToXML(writer, "NotificationAreaIcon", this.NotificationAreaIcon);
            XmlHelper.WriteElementToXML(writer, "VideoExtensions", this.VideoExtensionsString);
            XmlHelper.WriteElementToXML(writer, "OtherExtensions", this.OtherExtensionsString);
            XmlHelper.WriteElementToXML(writer, "ExportRSSMaxDays", this.ExportRSSMaxDays);
            XmlHelper.WriteElementToXML(writer, "ExportRSSMaxShows", this.ExportRSSMaxShows);
            XmlHelper.WriteElementToXML(writer, "ExportRSSDaysPast", this.ExportRSSDaysPast);
            XmlHelper.WriteElementToXML(writer, "KeepTogether", this.KeepTogether);
            XmlHelper.WriteElementToXML(writer, "KeepTogetherType", (int)this.KeepTogetherMode);
            XmlHelper.WriteElementToXML(writer, "KeepTogetherExtensions", this.KeepTogetherExtensionsString);
            XmlHelper.WriteElementToXML(writer, "LeadingZeroOnSeason", this.LeadingZeroOnSeason);
            XmlHelper.WriteElementToXML(writer, "ShowInTaskbar", this.ShowInTaskbar);
            XmlHelper.WriteElementToXML(writer, "IgnoreSamples", this.IgnoreSamples);
            XmlHelper.WriteElementToXML(writer, "ForceLowercaseFilenames", this.ForceLowercaseFilenames);
            XmlHelper.WriteElementToXML(writer, "RenameTxtToSub", this.RenameTxtToSub);
            XmlHelper.WriteElementToXML(writer, "ParallelDownloads", this.ParallelDownloads);
            XmlHelper.WriteElementToXML(writer, "AutoSelectShowInMyShows", this.AutoSelectShowInMyShows);
            XmlHelper.WriteElementToXML(writer, "AutoCreateFolders", this.AutoCreateFolders);
            XmlHelper.WriteElementToXML(writer, "ShowEpisodePictures", this.ShowEpisodePictures);
            XmlHelper.WriteElementToXML(writer, "HideWtWSpoilers", this.HideWtWSpoilers);
            XmlHelper.WriteElementToXML(writer, "HideMyShowsSpoilers", this.HideMyShowsSpoilers);
            XmlHelper.WriteElementToXML(writer, "SpecialsFolderName", this.SpecialsFolderName);
            XmlHelper.WriteElementToXML(writer, "uTorrentPath", this.uTorrentPath);
            XmlHelper.WriteElementToXML(writer, "ResumeDatPath", this.ResumeDatPath);
            XmlHelper.WriteElementToXML(writer, "SearchRSS", this.SearchRSS);
            XmlHelper.WriteElementToXML(writer, "EpImgs", this.EpTBNs);
            XmlHelper.WriteElementToXML(writer, "NFOShows", this.NFOShows);
            XmlHelper.WriteElementToXML(writer, "NFOEpisodes", this.NFOEpisodes);
            XmlHelper.WriteElementToXML(writer, "KODIImages", this.KODIImages);
            XmlHelper.WriteElementToXML(writer, "pyTivoMeta", this.pyTivoMeta);
            XmlHelper.WriteElementToXML(writer, "pyTivoMetaSubFolder", this.pyTivoMetaSubFolder);
            XmlHelper.WriteElementToXML(writer, "FolderJpg", this.FolderJpg);
            XmlHelper.WriteElementToXML(writer, "FolderJpgIs", (int)this.FolderJpgIs);
            XmlHelper.WriteElementToXML(writer, "MonitoredFoldersScanType", (int)this.MonitoredFoldersScanType);
            XmlHelper.WriteElementToXML(writer, "SelectedKODIType", (int)this.SelectedKODIType);
            XmlHelper.WriteElementToXML(writer, "CheckuTorrent", this.CheckuTorrent);
            XmlHelper.WriteElementToXML(writer, "RenameCheck", this.RenameCheck);
            XmlHelper.WriteElementToXML(writer, "PreventMove", this.PreventMove);
            XmlHelper.WriteElementToXML(writer, "MissingCheck", this.MissingCheck);
            XmlHelper.WriteElementToXML(writer, "AutoSearchForDownloadedFiles", this.AutoSearchForDownloadedFiles);
            XmlHelper.WriteElementToXML(writer, "UpdateFileDates", this.CorrectFileDates);
            XmlHelper.WriteElementToXML(writer, "SearchLocally", this.SearchLocally);
            XmlHelper.WriteElementToXML(writer, "LeaveOriginals", this.LeaveOriginals);
            XmlHelper.WriteElementToXML(writer, "LookForDateInFilename", this.LookForDateInFilename);
            XmlHelper.WriteElementToXML(writer, "AutoMergeEpisodes", this.AutoMergeEpisodes);
            XmlHelper.WriteElementToXML(writer, "MonitorFolders", this.MonitorFolders);
            XmlHelper.WriteElementToXML(writer, "StartupScan", this.runStartupCheck);
            XmlHelper.WriteElementToXML(writer, "PeriodicScan", this.runPeriodicCheck);
            XmlHelper.WriteElementToXML(writer, "PeriodicScanHours", this.periodCheckHours);
            XmlHelper.WriteElementToXML(writer, "RemoveDownloadDirectoriesFiles", this.RemoveDownloadDirectoriesFiles);
            XmlHelper.WriteElementToXML(writer, "SABAPIKey", this.SABAPIKey);
            XmlHelper.WriteElementToXML(writer, "CheckSABnzbd", this.CheckSABnzbd);
            XmlHelper.WriteElementToXML(writer, "SABHostPort", this.SABHostPort);
            XmlHelper.WriteElementToXML(writer, "PreferredLanguage", this.PreferredLanguage);
            XmlHelper.WriteElementToXML(writer, "WTWDoubleClick", (int)this.WTWDoubleClick);
            XmlHelper.WriteElementToXML(writer, "EpJPGs", this.EpJPGs);
            XmlHelper.WriteElementToXML(writer, "SeriesJpg", this.SeriesJpg);
            XmlHelper.WriteElementToXML(writer, "Mede8erXML", this.Mede8erXML);
            XmlHelper.WriteElementToXML(writer, "ShrinkLargeMede8erImages", this.ShrinkLargeMede8erImages);
            XmlHelper.WriteElementToXML(writer, "FanArtJpg", this.FanArtJpg);
            XmlHelper.WriteElementToXML(writer, "DeleteEmpty", this.Tidyup.DeleteEmpty);
            XmlHelper.WriteElementToXML(writer, "DeleteEmptyIsRecycle", this.Tidyup.DeleteEmptyIsRecycle);
            XmlHelper.WriteElementToXML(writer, "EmptyIgnoreWords", this.Tidyup.EmptyIgnoreWords);
            XmlHelper.WriteElementToXML(writer, "EmptyIgnoreWordList", this.Tidyup.EmptyIgnoreWordList);
            XmlHelper.WriteElementToXML(writer, "EmptyIgnoreExtensions", this.Tidyup.EmptyIgnoreExtensions);
            XmlHelper.WriteElementToXML(writer, "EmptyIgnoreExtensionList", this.Tidyup.EmptyIgnoreExtensionList);
            XmlHelper.WriteElementToXML(writer, "EmptyMaxSizeCheck", this.Tidyup.EmptyMaxSizeCheck);
            XmlHelper.WriteElementToXML(writer, "EmptyMaxSizeMB", this.Tidyup.EmptyMaxSizeMB);
            XmlHelper.WriteElementToXML(writer, "BetaMode", (int)this.Mode);
            XmlHelper.WriteElementToXML(writer, "PercentDirtyUpgrade", this.UpgradeDirtyPercent);
            XmlHelper.WriteElementToXML(writer, "BaseSeasonName", this.defaultSeasonWord);
            XmlHelper.WriteElementToXML(writer, "SearchSeasonNames", this.searchSeasonWordsString);

            XmlHelper.WriteElementToXML(writer, "BulkAddIgnoreRecycleBin", this.BulkAddIgnoreRecycleBin);
            XmlHelper.WriteElementToXML(writer, "BulkAddCompareNoVideoFolders", this.BulkAddCompareNoVideoFolders);
            XmlHelper.WriteElementToXML(writer, "AutoAddMovieTerms", this.AutoAddMovieTerms);
            XmlHelper.WriteElementToXML(writer, "AutoAddIgnoreSuffixes", this.AutoAddIgnoreSuffixes);

            writer.WriteStartElement("FNPRegexs");
            foreach (FilenameProcessorRegEx re in this.FNPRegexs)
            {
                writer.WriteStartElement("Regex");
                XmlHelper.WriteAttributeToXML(writer, "Enabled", re.Enabled);
                XmlHelper.WriteAttributeToXML(writer, "RE", re.RegEx);
                XmlHelper.WriteAttributeToXML(writer, "UseFullPath", re.UseFullPath);
                XmlHelper.WriteAttributeToXML(writer, "Notes", re.Notes);
                writer.WriteEndElement(); // Regex
            }
            writer.WriteEndElement(); // FNPRegexs

            writer.WriteStartElement("RSSURLs");
            foreach (string s in this.RSSURLs) XmlHelper.WriteElementToXML(writer, "URL", s);
            writer.WriteEndElement(); // RSSURLs

            if (this.ShowStatusColors != null)
            {
                writer.WriteStartElement("ShowStatusTVWColors");
                foreach (KeyValuePair<ShowStatusColoringType, System.Drawing.Color> e in this.ShowStatusColors)
                {
                    writer.WriteStartElement("ShowStatusTVWColor");
                    // TODO ... Write Meta Flags
                   XmlHelper.WriteAttributeToXML(writer, "IsMeta", e.Key.IsMetaType);
                   XmlHelper.WriteAttributeToXML(writer, "IsShowLevel", e.Key.IsShowLevel);
                   XmlHelper.WriteAttributeToXML(writer, "ShowStatus", e.Key.Status);
                    XmlHelper.WriteAttributeToXML(writer, "Color", ColorHelper.TranslateColorToHtml(e.Value));
                    writer.WriteEndElement(); //ShowStatusTVWColor
                }
                writer.WriteEndElement(); // ShowStatusTVWColors
            }

            if (this.Filter != null)
            {
                writer.WriteStartElement("ShowFilters");

                XmlHelper.WriteInfo(writer, "NameFilter", "Name", this.Filter.ShowName);
                XmlHelper.WriteInfo(writer, "ShowStatusFilter", "ShowStatus", this.Filter.ShowStatus);
                XmlHelper.WriteInfo(writer, "ShowNetworkFilter", "ShowNetwork", this.Filter.ShowNetwork);
                XmlHelper.WriteInfo(writer, "ShowRatingFilter", "ShowRating", this.Filter.ShowRating);

                foreach (string genre in this.Filter.Genres) XmlHelper.WriteInfo(writer, "GenreFilter", "Genre", genre);

                writer.WriteEndElement(); //ShowFilters
            }

            writer.WriteEndElement(); // settings
        }

        internal float PercentDirtyUpgrade()
        {
            return this.UpgradeDirtyPercent;
        }

        public FolderJpgIsType ItemForFolderJpg() => this.FolderJpgIs;

        public string GetVideoExtensionsString() => this.VideoExtensionsString;

        public string GetOtherExtensionsString() => this.OtherExtensionsString;

        public string GetKeepTogetherString() => this.KeepTogetherExtensionsString;


        public bool RunPeriodicCheck() => this.runPeriodicCheck;
        public int PeriodicCheckPeriod() => this.periodCheckHours * 60 * 60 * 1000;
        public bool RunOnStartUp() => this.runStartupCheck;

        public string GetSeasonSearchTermsString() => this.searchSeasonWordsString;

        public static bool OKExtensionsString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return true;

            string[] t = s.Split(';');
            foreach (string s2 in t)
            {
                if ((string.IsNullOrEmpty(s2)) || (!s2.StartsWith(".")))
                    return false;
            }
            return true;
        }

        public static string CompulsoryReplacements()
        {
            return "*?<>:/\\|\""; // invalid filename characters, must be in the list!
        }

        public static List<FilenameProcessorRegEx> DefaultFNPList()
        {
            List<FilenameProcessorRegEx> l = new List<FilenameProcessorRegEx>
            {
                new FilenameProcessorRegEx(
                    "(^|[^a-z])s?(?<s>[0-9]+).?[ex](?<e>[0-9]{2,})(-?e[0-9]{2,})*-?[ex](?<f>[0-9]{2,})[^a-z]",
                    false, true, "Multipart Rule : s04e01e02e03 S01E01-E02"),
                new FilenameProcessorRegEx("(^|[^a-z])s?(?<s>[0-9]+)[ex](?<e>[0-9]{2,})(e[0-9]{2,})*[^a-z]",
                    false, true, "3x23 s3x23 3e23 s3e23 s04e01e02e03"),
                new FilenameProcessorRegEx("(^|[^a-z])s?(?<s>[0-9]+)(?<e>[0-9]{2,})[^a-z]",
                    false, false, "323 or s323 for season 3, episode 23. 2004 for season 20, episode 4."),
                new FilenameProcessorRegEx("(^|[^a-z])s(?<s>[0-9]+)--e(?<e>[0-9]{2,})[^a-z]",
                    false, false, "S02--E03"),
                new FilenameProcessorRegEx("(^|[^a-z])s(?<s>[0-9]+) e(?<e>[0-9]{2,})[^a-z]",
                    false, false, "'S02.E04' and 'S02 E04'"),
                new FilenameProcessorRegEx("^(?<s>[0-9]+) (?<e>[0-9]{2,})",
                    false, false, "filenames starting with '1.23' for season 1, episode 23"),
                new FilenameProcessorRegEx("(^|[^a-z])(?<s>[0-9])(?<e>[0-9]{2,})[^a-z]",
                    false, false, "Show - 323 - Foo"),
                new FilenameProcessorRegEx("(^|[^a-z])se(?<s>[0-9]+)([ex]|ep|xep)?(?<e>[0-9]+)[^a-z]",
                    false, true, "se3e23 se323 se1ep1 se01xep01..."),
                new FilenameProcessorRegEx("(^|[^a-z])(?<s>[0-9]+)-(?<e>[0-9]{2,})[^a-z]",
                    false, true, "3-23 EpName"),
                new FilenameProcessorRegEx("(^|[^a-z])s(?<s>[0-9]+) +- +e(?<e>[0-9]{2,})[^a-z]",
                    false, true, "ShowName - S01 - E01"),
                new FilenameProcessorRegEx("\\b(?<e>[0-9]{2,}) ?- ?.* ?- ?(?<s>[0-9]+)",
                    false, true, "like '13 - Showname - 2 - Episode Title.avi'"),
                new FilenameProcessorRegEx("\\b(episode|ep|e) ?(?<e>[0-9]{2,}) ?- ?(series|season) ?(?<s>[0-9]+)",
                    false, true, "episode 3 - season 4"),
                new FilenameProcessorRegEx("season (?<s>[0-9]+)\\\\e?(?<e>[0-9]{1,3}) ?-",
                    true, true, "Show Season 3\\E23 - Epname"),
                new FilenameProcessorRegEx("season (?<s>[0-9]+)\\\\episode (?<e>[0-9]{1,3})",
                    true, false, "Season 3\\Episode 23")
            };

            return l;
        }

        private static List<Replacement> DefaultListRE()
        {
            return new List<Replacement>
                       {
                           new Replacement("*", "#", false),
                           new Replacement("?", "", false),
                           new Replacement(">", "", false),
                           new Replacement("<", "", false),
                           new Replacement(":", "-", false),
                           new Replacement("/", "-", false),
                           new Replacement("\\", "-", false),
                           new Replacement("|", "-", false),
                           new Replacement("\"", "'", false)
                       };
        }

        private static List<string> DefaultRSSURLList()
        {
            List<string> sl = new List<string>();
            return sl;
        }

        public static string[] TabNames()
        {
            return new[] { "MyShows", "Scan", "WTW" };
        }

        public static string TabNameForNumber(int n)
        {
            if ((n >= 0) && (n < TabNames().Length))
                return TabNames()[n];
            return "";
        }

        public static int TabNumberFromName(string n)
        {
            int r = 0;
            if (!string.IsNullOrEmpty(n))
                r = Array.IndexOf(TabNames(), n);
            if (r < 0)
                r = 0;
            return r;
        }

        public bool UsefulExtension(string sn, bool otherExtensionsToo)
        {
            foreach (string s in this.VideoExtensionsArray)
            {
                if (sn.ToLower() == s)
                    return true;
            }
            if (otherExtensionsToo)
            {
                foreach (string s in this.OtherExtensionsArray)
                {
                    if (sn.ToLower() == s)
                        return true;
                }
            }

            return false;
        }

        public bool KeepExtensionTogether(string extension)
        {
            if (KeepTogether == false) return false;

            if (KeepTogetherMode == KeepTogetherModes.All) return true;

            if (KeepTogetherMode == KeepTogetherModes.Just) return KeepTogetherExtensionsList.Contains(extension);

            if (KeepTogetherMode == KeepTogetherModes.AllBut) return !KeepTogetherExtensionsList.Contains(extension);

            // TODO: Put this back
            //logger.Error("INVALID USE OF KEEP EXTENSION");
            return false;
        }

        public string BtSearchUrl(ProcessedEpisode epi)
        {
            TheTvDbSeries s = epi?.TheSeries;
            if (s == null)
            {
                return "";
            }

            string url = (epi.SI.UseCustomSearchURL && !string.IsNullOrWhiteSpace(epi.SI.CustomSearchURL))
                ? epi.SI.CustomSearchURL
                : TheSearchers.CurrentSearchUrl();

            return CustomName.NameForNoExt(epi, url, true);
        }

        public string FilenameFriendly(string fn)
        {
            if (string.IsNullOrWhiteSpace(fn)) return "";

            foreach (Replacement rep in this.Replacements)
            {
                if (rep.IgnoreCase)
                    fn = Regex.Replace(fn, Regex.Escape(rep.This), Regex.Escape(rep.That), RegexOptions.IgnoreCase);
                else
                    fn = fn.Replace(rep.This, rep.That);
            }
            if (this.ForceLowercaseFilenames)
                fn = fn.ToLower();
            return fn;
        }

        public bool NeedToDownloadBannerFile()
        {
            // Return true iff we need to download season specific images
            // There are 4 possible reasons
            return (SeasonSpecificFolderJPG() || this.KODIImages || this.SeriesJpg || this.FanArtJpg);
        }

        // ReSharper disable once InconsistentNaming
        public bool SeasonSpecificFolderJPG()
        {
            return (FolderJpgIsType.SeasonPoster == this.FolderJpgIs);
        }

        public bool DownloadFrodoImages()
        {
            return (this.KODIImages && (this.SelectedKODIType == KODIType.Both || this.SelectedKODIType == KODIType.Frodo));
        }

        public bool DownloadEdenImages()
        {
            return (this.KODIImages && (this.SelectedKODIType == KODIType.Both || this.SelectedKODIType == KODIType.Eden));
        }

        public bool KeepTogetherFilesWithType(string fileExtension)
        {
            if (KeepTogether == false) return false;

            switch (KeepTogetherMode)
            {
                case KeepTogetherModes.All: return true;
                case KeepTogetherModes.Just: return KeepTogetherExtensionsList.Contains(fileExtension);
                case KeepTogetherModes.AllBut: return !KeepTogetherExtensionsList.Contains(fileExtension);

            }

            return true;
        }
    }
}
