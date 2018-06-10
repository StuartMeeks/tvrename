using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Newtonsoft.Json.Linq;
using TVRename.AppLogic.BitTorrent;
using TVRename.AppLogic.Delegates;
using TVRename.AppLogic.DownloadIdentifiers;
using TVRename.AppLogic.Extensions;
using TVRename.AppLogic.FileSystemCache;
using TVRename.AppLogic.Finders;
using TVRename.AppLogic.FolderMonitor;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.ScanItems.Actions;
using TVRename.AppLogic.ScanItems.Items;
using TVRename.AppLogic.Settings;
using TVRename.AppLogic.TheTvDb;

namespace TVRename.AppLogic
{
    public class TvRenameManager : IDisposable
    {

        #region Private Members

        private ItemList _actionList;
        private readonly List<ProcessedSeries> _processedSeries;
        private readonly List<IgnoreItem> _ignoreItems;
        private readonly List<FinderBase> _finders;

        private readonly DownloadIdentifiersController _downloadIdentifiers;
        private FolderMonitorEntryList _addItems;
        private Statistics _statistics;

        private List<string> _monitorFolders;
        private List<string> _ignoreFolders;
        private IEnumerable<string> _seasonWordsCache;

        private bool _actionStarting;
        private bool _actionPause;
        private bool _actionCancel;

        private bool _downloadDone;
        private bool _downloadOk;
        private bool _downloadStopOnError;
        private int _downloadsRemaining;
        private int _downloadPercent;

        private Thread _downloaderThread;
        private Thread _actionProcessorThread;
        private Semaphore[] _actionSemaphores;
        private List<Thread> _actionWorkers;

        private Semaphore _workerSemaphore;
        private List<Thread> _workers;

        private bool _busy;
        private bool _dirty;

        private string _loadError;
        private bool _loadOk;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly NLog.Logger Threadslogger = NLog.LogManager.GetLogger("threads");

        #endregion

        #region Public Properties

        public CommandLineArgs Args { get; }

        public List<string> SearchFolders { get; private set; }

        public IEnumerable<string> SeasonWords => _seasonWordsCache ?? (_seasonWordsCache = GetSeasonWords());



        #endregion


        // TODO: Get this out of here
        // public ScanProgress ScanProgDlg;

        #region Constructor

        public TvRenameManager(FileInfo settingsFile, CommandLineArgs args)
        {
            Args = args;

            _processedSeries = new List<ProcessedSeries>();
            _actionList = new ItemList();
            _ignoreItems = new List<IgnoreItem>();
            _finders = new List<FinderBase>
            {
                new FileFinder(this),
                // TODO: Add back the three finders below
                //new RSSFinder(this),
                //new uTorrentFinder(this),
                //new SABnzbdFinder(this)
            };

            _downloadIdentifiers = new DownloadIdentifiersController();
            _addItems = new FolderMonitorEntryList();
            _statistics = new Statistics();

            _monitorFolders = new List<string>();
            _ignoreFolders = new List<string>();
            SearchFolders = new List<string>();

            _actionStarting = false;
            _actionPause = false;
            _actionCancel = false;

            _downloadDone = true;
            _downloadOk = true;

            _workerSemaphore = null;
            _workers = null;

            _busy = false;
            _dirty = false;

            _loadOk = (settingsFile == null || LoadXmlSettings(settingsFile)) && TheTvDbClient.Instance.LoadOK;

            UpdateTvDbLanguage();
        }

        #endregion

        private static void UpdateTvDbLanguage()
        {
            TheTvDbClient.Instance.RequestLanguage = ApplicationSettings.Instance.PreferredLanguage;
        }

        #region Get all the things

        private IEnumerable<string> GetSeasonWords()
        {
            //See https://github.com/TV-Rename/tvrename/issues/241 for background
            var seasonWordsFromShows = from si in _processedSeries select si.AutoAdd_SeasonFolderName.Trim();
            var results = seasonWordsFromShows.Distinct().ToList();

            results.Add(ApplicationSettings.Instance.defaultSeasonWord);
            results.AddRange(ApplicationSettings.Instance.searchSeasonWordsArray);

            return results.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct();
        }

        public List<string> GetGenres()
        {
            var allGenres = new List<string>();
            foreach (var processedSeries in _processedSeries)
            {
                if (processedSeries.Genres != null) allGenres.AddRange(processedSeries.Genres);
            }
            var distinctGenres = allGenres.Distinct().ToList();
            distinctGenres.Sort();
            return distinctGenres;
        }

        public List<string> GetStatuses()
        {
            var allStatuses = new List<string>();
            foreach (var processedSeries in _processedSeries)
            {
                if (processedSeries.ShowStatus != null) allStatuses.Add(processedSeries.ShowStatus);
            }
            var distinctStatuses = allStatuses.Distinct().ToList();
            distinctStatuses.Sort();
            return distinctStatuses;
        }

        public List<string> GetNetworks()
        {
            var allValues = new List<string>();
            foreach (var processedSeries in _processedSeries)
            {
                if (processedSeries.TheSeries()?.getNetwork() != null)
                {
                    allValues.Add(processedSeries.TheSeries().getNetwork());
                }
            }

            var distinctValues = allValues.Distinct().ToList();
            distinctValues.Sort();
            return distinctValues;
        }

        public List<string> GetContentRatings()
        {
            var allValues = new List<string>();
            foreach (var processedSeries in _processedSeries)
            {
                if (processedSeries.TheSeries()?.GetContentRating() != null)
                {
                    allValues.Add(processedSeries.TheSeries().GetContentRating());
                }
            }
            var distinctValues = allValues.Distinct().ToList();
            distinctValues.Sort();
            return distinctValues;
        }

        public int GetMinYear() => _processedSeries.Min(si => Convert.ToInt32(si.TheSeries().GetYear()));

        public int GetMaxYear() => _processedSeries.Max(si => Convert.ToInt32(si.TheSeries().GetYear()));

        public Statistics GetStatistics()
        {
            _statistics.NumberOfShows = _processedSeries.Count;
            _statistics.NumberOfSeasons = 0;
            _statistics.NumberOfEpisodesExpected = 0;

            foreach (var processedSeries in _processedSeries)
            {
                foreach (var seasonEpisode in processedSeries.SeasonEpisodes)
                {
                    _statistics.NumberOfEpisodesExpected += seasonEpisode.Value.Count;
                }

                _statistics.NumberOfSeasons += processedSeries.SeasonEpisodes.Count;
            }

            return _statistics;
        }

        public IEnumerable<ProcessedSeries> GetShowItems()
        {
            _processedSeries.Sort(ProcessedSeries.CompareShowItemNames);
            return _processedSeries;
        }

        public ProcessedSeries GetShowItem(int id)
        {
            foreach (var processedSeries in _processedSeries)
            {
                if (processedSeries.TVDBCode == id)
                {
                    return processedSeries;
                }
            }
            return null;
        }

        #endregion

        internal static bool FindSeasEp(FileInfo theFile, out int seasF, out int epF, out int maxEp, ProcessedSeries sI)
        {
            return FindSeasEp(theFile, out seasF, out epF, out maxEp, sI, out _);
        }

        public void SetSearcher(int n)
        {
            ApplicationSettings.Instance.TheSearchers.SetToNumber(n);
            _dirty = true;
        }

        public bool HasSeasonFolders(DirectoryInfo di, out string folderName, out DirectoryInfo[] subDirs)
        {
            try
            {
                subDirs = di.GetDirectories();
                // keep in sync with ProcessAddItems, etc.
                foreach (var sw in GetSeasonWords())
                {
                    foreach (var subDir in subDirs)
                    {
                        if (subDir.Name.Contains(sw, StringComparison.InvariantCultureIgnoreCase))
                        {
                            Logger.Info("Assuming {0} contains a show because keyword '{1}' is found in subdirectory {2}", di.FullName, sw, subDir.FullName);
                            folderName = sw;
                            return true;

                        }
                    }
                }

            }
            catch (UnauthorizedAccessException)
            {
                // e.g. recycle bin, system volume information
                Logger.Warn("Could not access {0} (or a subdir), may not be an issue as could be expected e.g. recycle bin, system volume information", di.FullName);
                subDirs = null;
            }


            folderName = null;
            return false;
        }

        public bool CheckFolderForShows(DirectoryInfo di2, bool andGuess, out DirectoryInfo[] subDirs)
        {
            // ..and not already a folder for one of our shows
            var theFolder = di2.FullName.ToLower();
            foreach (var si in _processedSeries)
            {
                if (si.AutoAddNewSeasons && !string.IsNullOrEmpty(si.AutoAdd_FolderBase) && theFolder.IsSubfolderOf(si.AutoAdd_FolderBase))
                {
                    // we're looking at a folder that is a subfolder of an existing show
                    Logger.Info("Rejecting {0} as it's already part of {1}.", theFolder, si.ShowName);
                    subDirs = null;
                    return true;
                }

                if (!si.UsesManualFolders())
                {
                    continue;
                }

                var afl = si.AllFolderLocations();
                foreach (var kvp in afl)
                {
                    foreach (var folder in kvp.Value)
                    {
                        if (!string.Equals(theFolder, folder, StringComparison.CurrentCultureIgnoreCase))
                        {
                            continue;
                        }

                        Logger.Info("Rejecting {0} as it's already part of {1}:{2}.", theFolder, si.ShowName, folder);
                        subDirs = null;
                        return true;
                    }
                }
            } // for each showitem


            //We don't have it already
            bool hasSeasonFolders;
            try
            {
                hasSeasonFolders = HasSeasonFolders(di2, out var folderName, out var subDirectories);

                subDirs = subDirectories;

                //This is an indication that something is wrong
                if (subDirectories is null)
                {
                    return false;
                }

                var hasSubFolders = subDirectories.Length > 0;
                if (!hasSubFolders || hasSeasonFolders)
                {
                    if (ApplicationSettings.Instance.BulkAddCompareNoVideoFolders && !HasFilmFiles(di2))
                    {
                        return false;
                    }

                    if (ApplicationSettings.Instance.BulkAddIgnoreRecycleBin &&
                        di2.FullName.Contains("$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // ....its good!
                    var ai = new FolderMonitorEntry(di2.FullName, hasSeasonFolders, folderName);
                    _addItems.Add(ai);
                    Logger.Info("Adding {0} as a new folder", theFolder);
                    if (andGuess)
                    {
                        GuessShowItem(ai);
                    }
                }

            }
            catch (UnauthorizedAccessException)
            {
                Logger.Info("Can't access {0}, so ignoring it", di2.FullName);
                subDirs = null;
                return true;
            }

            return hasSeasonFolders;
        }

        private static bool HasFilmFiles(DirectoryInfo directory)
        {
            return directory.GetFiles("*", SearchOption.TopDirectoryOnly).Any(file => ApplicationSettings.Instance.UsefulExtension(file.Extension, false));
        }

        public void CheckFolderForShows(DirectoryInfo di, ref bool stop)
        {
            // is it on the ''Bulk Add Shows' ignore list?
            if (_ignoreFolders.Contains(di.FullName.ToLower()))
            {
                Logger.Info("Rejecting {0} as it's on the ignore list.", di.FullName);
                return;
            }

            if (CheckFolderForShows(di, false, out var subDirs))
            {
                return; // done.
            }

            if (subDirs == null)
            {
                return; //indication we could not access the subdirectory
            }

            // recursively check a folder for new shows

            foreach (var di2 in subDirs)
            {
                if (stop)
                {
                    return;
                }

                CheckFolderForShows(di2, ref stop); // not a season folder.. recurse!
            } // for each directory
        }

        public void AddAllToMyShows()
        {
            foreach (var ai in _addItems)
            {
                if (ai.CodeUnknown)
                {
                    continue; // skip
                }

                // see if there is a matching show item
                var found = ShowItemForCode(ai.TvDbCode);
                if (found == null)
                {
                    // need to add a new showitem
                    found = new ProcessedSeries(ai.TvDbCode);
                    _processedSeries.Add(found);
                }

                found.AutoAdd_FolderBase = ai.Folder;
                found.AutoAdd_FolderPerSeason = ai.HasSeasonFoldersGuess;

                found.AutoAdd_SeasonFolderName = ai.SeasonFolderName + " ";
                GetStatistics().AutoAddedShows++;
            }

            GenDict();
            _addItems.Clear();
            ExportShowInfo();
        }

        public void GuessShowItem(FolderMonitorEntry ai)
        {
            var showName = GuessShowName(ai);

            if (string.IsNullOrEmpty(showName))
            {
                return;
            }

            TheTvDbClient.Instance.GetLock("GuessShowItem");

            var ser = TheTvDbClient.Instance.FindSeriesForName(showName);
            if (ser != null)
            {
                ai.TvDbCode = ser.TVDBCode;
            }

            TheTvDbClient.Instance.Unlock("GuessShowItem");
        }

        public void CheckFolders(ref bool stop, ref int percentDone)
        {
            // Check the  folder list, and build up a new "AddItems" list.
            // guessing what the shows actually are isn't done here.  That is done by
            // calls to "GuessShowItem"
            Logger.Info("*********************************************************************");
            Logger.Info("*Starting to find folders that contain files, but are not in library*");

            _addItems = new FolderMonitorEntryList();

            var c = _monitorFolders.Count;
            var c2 = 0;

            foreach (var folder in _monitorFolders)
            {
                percentDone = 100 * c2++ / c;
                var di = new DirectoryInfo(folder);
                if (!di.Exists)
                {
                    continue;
                }

                CheckFolderForShows(di, ref stop);

                if (stop)
                {
                    break;
                }
            }
        }

        public bool RenameFilesToMatchTorrent(string torrent, string folder, ProgressUpdatedDelegate prog, bool copyNotMove, string copyDest, CommandLineArgs args)
        {
            if (string.IsNullOrEmpty(folder))
            {
                return false;
            }

            if (string.IsNullOrEmpty(torrent))
            {
                return false;
            }

            if (copyNotMove)
            {
                if (string.IsNullOrEmpty(copyDest))
                {
                    return false;
                }

                if (!Directory.Exists(copyDest))
                {
                    return false;
                }
            }

            GetStatistics().TorrentsMatched++;

            var btp = new BtFileRenamer(prog);
            var newList = new ItemList();
            var r = btp.RenameFilesOnDiskToMatchTorrent(torrent, folder, newList, copyNotMove, copyDest);

            foreach (var i in newList)
            {
                _actionList.Add(i);
            }

            return r;
        }

        public void GetThread(object codeIn)
        {
            System.Diagnostics.Debug.Assert(_workerSemaphore != null);

            try
            {
                _workerSemaphore.WaitOne(); // don't start until we're allowed to

                var code = (int)codeIn;
                var bannersToo = ApplicationSettings.Instance.NeedToDownloadBannerFile();

                Threadslogger.Trace("  Downloading " + code);
                var r = TheTvDbClient.Instance.EnsureUpdated(code, bannersToo);
                Threadslogger.Trace("  Finished " + code);

                if (!r)
                {
                    _downloadOk = false;
                    if (_downloadStopOnError)
                    {
                        _downloadDone = true;
                    }
                }

                _workerSemaphore.Release(1);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Unhandled Exception in GetThread");
            }
        }

        public void WaitForAllThreadsAndTidyUp()
        {
            if (_workers != null)
            {
                foreach (var t in _workers)
                {
                    if (t.IsAlive)
                    {
                        t.Join();
                    }
                }
            }

            _workers = null;
            _workerSemaphore = null;
        }

        public void Downloader()
        {
            // do background downloads of webpages
            Logger.Info("*******************************");
            Logger.Info("Starting Background Download...");
            try
            {
                if (_processedSeries.Count == 0)
                {
                    _downloadDone = true;
                    _downloadOk = true;
                    return;
                }

                if (!TheTvDbClient.Instance.GetUpdates())
                {
                    _downloadDone = true;
                    _downloadOk = false;
                    return;
                }

                // for eachs of the ShowItems, make sure we've got downloaded data for it
                var totalItems = _processedSeries.Count;
                var n = 0;
                var codes = new List<int>();
                foreach (var processedSeries in _processedSeries)
                {
                    codes.Add(processedSeries.TVDBCode);
                }

                var numWorkers = ApplicationSettings.Instance.ParallelDownloads;
                Logger.Info("Setting up {0} threads to download information from TVDB.com", numWorkers);
                _workers = new List<Thread>();
                _workerSemaphore = new Semaphore(numWorkers, numWorkers); // allow up to numWorkers working at once

                foreach (var code in codes)
                {
                    _downloadPercent = 100 * (n + 1) / (totalItems + 1);
                    _downloadsRemaining = totalItems - n;
                    n++;

                    _workerSemaphore.WaitOne(); // blocks until there is an available slot
                    var t = new Thread(GetThread);
                    _workers.Add(t);
                    t.Name = "GetThread:" + code;
                    t.Start(code); // will grab the semaphore as soon as we make it available
                    var nfr = _workerSemaphore.Release(1); // release our hold on the semaphore, so that worker can grab it
                    Threadslogger.Trace("Started " + code + " pool has " + nfr + " free");
                    Thread.Sleep(1); // allow the other thread a chance to run and grab

                    // tidy up any finished workers
                    for (var i = _workers.Count - 1; i >= 0; i--)
                    {
                        if (!_workers[i].IsAlive)
                        {
                            _workers.RemoveAt(i); // remove dead worker
                        }
                    }

                    if (_downloadDone)
                    {
                        break;
                    }
                }

                WaitForAllThreadsAndTidyUp();

                TheTvDbClient.Instance.UpdatesDoneOK();
                _downloadDone = true;
                _downloadOk = true;
            }
            catch (ThreadAbortException taa)
            {
                _downloadDone = true;
                _downloadOk = false;
                Logger.Error(taa);
            }
            catch (Exception e)
            {
                _downloadDone = true;
                _downloadOk = false;
                Logger.Fatal(e, "UNHANDLED EXCEPTION IN DOWNLOAD THREAD");
            }
            finally
            {
                _workers = null;
                _workerSemaphore = null;
            }
        }

        public void StartBgDownloadThread(bool stopOnError)
        {
            if (!_downloadDone)
            {
                return;
            }

            _downloadStopOnError = stopOnError;
            _downloadPercent = 0;
            _downloadDone = false;
            _downloadOk = true;
            _downloaderThread = new Thread(Downloader) { Name = "Downloader" };
            _downloaderThread.Start();
        }

        public void WaitForBgDownloadDone()
        {
            if (_downloaderThread != null && _downloaderThread.IsAlive)
            {
                _downloaderThread.Join();
            }

            _downloaderThread = null;
        }

        public void StopBgDownloadThread()
        {
            if (_downloaderThread == null)
            {
                return;
            }

            _downloadDone = true;
            _downloaderThread.Join();
            _downloaderThread = null;
        }

        public bool DoDownloadsFg()
        {
            if (ApplicationSettings.Instance.OfflineMode)
            {
                return true; // don't do internet in offline mode!
            }

            Logger.Info("Doing downloads in the foreground...");
            StartBgDownloadThread(true);

            const int delayStep = 100;
            var count = 1000 / delayStep; // one second
            while ((count-- > 0) && (!_downloadDone))
            {
                Thread.Sleep(delayStep);
            }

            if (!_downloadDone && !Args.Hide) // downloading still going on, so time to show the dialog if we're not in /hide mode
            {
                DownloadProgress dp = new DownloadProgress(this);
                dp.ShowDialog();
                dp.Update();
            }

            WaitForBgDownloadDone();

            TheTvDbClient.Instance.SaveCache();

            GenDict();

            if (!_downloadOk)
            {
                Logger.Warn(TheTvDbClient.Instance.LastError);
                if (!Args.Unattended && (!Args.Hide))
                {
                    MessageBox.Show(TheTvDbClient.Instance.LastError, "Error while downloading", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                TheTvDbClient.Instance.LastError = "";
            }

            return _downloadOk;
        }

        public bool GenDict()
        {
            var res = true;

            foreach (var si in _processedSeries)
            {
                if (!GenerateEpisodeDict(si))
                {
                    res = false;
                }
            }

            return res;
        }

        public Searchers GetSearchers()
        {
            return ApplicationSettings.Instance.TheSearchers;
        }

        public void TidyTvDb()
        {
            // remove any shows from thetvdb that aren't in My Shows
            TheTvDbClient.Instance.GetLock("TidyTVDB");
            var removeList = new List<int>();

            foreach (var kvp in TheTvDbClient.Instance.GetSeriesDict())
            {
                var found = false;
                foreach (var si in _processedSeries)
                {
                    if (si.TVDBCode != kvp.Key)
                    {
                        continue;
                    }

                    found = true;
                    break;
                }

                if (!found)
                {
                    removeList.Add(kvp.Key);
                }
            }

            foreach (var i in removeList)
            {
                TheTvDbClient.Instance.ForgetShow(i, false);
            }

            TheTvDbClient.Instance.Unlock("TheTVDB");
            TheTvDbClient.Instance.SaveCache();
        }

        public void Closing()
        {
            StopBgDownloadThread();
            GetStatistics().Save();
        }

        public void DoBtSearch(ProcessedEpisode ep)
        {
            if (ep == null)
            {
                return;
            }
            SystemHelper.StartProcess(ApplicationSettings.Instance.BtSearchUrl(ep));
        }

        public void DoWhenToWatch(bool cachedOnly)
        {
            if (!cachedOnly && !DoDownloadsFg())
            {
                return;
            }

            if (cachedOnly)
            {
                GenDict();
            }
        }

        public static List<FileInfo> FindEpOnDisk(DirectoryFileCache dfc, ProcessedEpisode pe, bool checkDirectoryExist = true)
        {
            return FindEpOnDisk(dfc, pe.SI, pe, checkDirectoryExist);
        }

        public static List<FileInfo> FindEpOnDisk(DirectoryFileCache dfc, ProcessedSeries si, TheTvDbEpisode epi, bool checkDirectoryExist = true)
        {
            if (dfc == null)
            {
                dfc = new DirectoryFileCache();
            }

            var ret = new List<FileInfo>();
            var seasWanted = si.DVDOrder ? epi.TheDVDSeason.SeasonNumber : epi.TheAiredSeason.SeasonNumber;
            var epWanted = si.DVDOrder ? epi.DVDEpNum : epi.AiredEpNum;

            var snum = seasWanted;

            if (!si.AllFolderLocationsEpCheck(checkDirectoryExist).ContainsKey(snum))
            {
                return ret;
            }

            foreach (var folder in si.AllFolderLocationsEpCheck(checkDirectoryExist)[snum])
            {
                var files = dfc.LoadCacheFromFolder(folder);
                if (files == null)
                {
                    continue;
                }

                foreach (var fiTemp in files)
                {
                    if (!ApplicationSettings.Instance.UsefulExtension(fiTemp.Extension, false))
                    {
                        continue; // move on
                    }

                    if (!FindSeasEp(fiTemp, out var seasFound, out var epFound, out _, si))
                    {
                        continue;
                    }

                    if (seasFound == -1)
                    {
                        seasFound = seasWanted;
                    }

                    if (seasFound == seasWanted && epFound == epWanted)
                    {
                        ret.Add(fiTemp);
                    }
                }
            }

            return ret;
        }

        public static bool HasAnyAirdates(ProcessedSeries si, int snum)
        {
            var ser = TheTvDbClient.Instance.GetSeries(si.TVDBCode);
            if (ser == null)
            {
                return false;
            }

            var seasonsToUse = si.DVDOrder ? ser.DVDSeasons : ser.AiredSeasons;
            if (!seasonsToUse.ContainsKey(snum))
            {
                return false;
            }

            foreach (var e in seasonsToUse[snum].Episodes)
            {
                if (e.FirstAired != null)
                {
                    return true;
                }

            }

            return false;
        }

        public static bool GenerateEpisodeDict(ProcessedSeries si)
        {
            si.SeasonEpisodes.Clear();

            TheTvDbClient.Instance.GetLock("GenerateEpisodeDict");

            var ser = TheTvDbClient.Instance.GetSeries(si.TVDBCode);

            if (ser == null)
            {
                TheTvDbClient.Instance.Unlock("GenerateEpisodeDict");
                return false; // TODO: warn user
            }

            var r = true;
            var seasonsToUse = si.DVDOrder ? ser.DVDSeasons : ser.AiredSeasons;
            foreach (var kvp in seasonsToUse)
            {
                var pel = GenerateEpisodes(si, ser, kvp.Key, true);
                si.SeasonEpisodes[kvp.Key] = pel;
                if (pel == null)
                {
                    r = false;
                }
            }

            // now, go through and number them all sequentially
            var theKeys = new List<int>();
            foreach (var snum in seasonsToUse.Keys)
            {
                theKeys.Add(snum);
            }
            theKeys.Sort();

            var overallCount = 1;
            foreach (var snum in theKeys)
            {
                if (snum == 0) continue;

                foreach (var pe in si.SeasonEpisodes[snum])
                {
                    pe.OverallNumber = overallCount;
                    if (si.DVDOrder)
                    {
                        overallCount += 1 + pe.EpNum2 - pe.DVDEpNum;
                    }
                    else
                    {
                        overallCount += 1 + pe.EpNum2 - pe.AiredEpNum;
                    }
                }
            }

            TheTvDbClient.Instance.Unlock("GenerateEpisodeDict");

            return r;
        }

        public static List<ProcessedEpisode> GenerateEpisodes(ProcessedSeries si, TheTvDbSeries ser, int snum, bool applyRules)
        {
            if (ser == null)
            {
                return null;
            }

            var seasonsToUse = si.DVDOrder ? ser.DVDSeasons : ser.AiredSeasons;
            if (!seasonsToUse.ContainsKey(snum))
            {
                return null; // todo.. something?
            }

            var seas = seasonsToUse[snum];
            if (seas == null)
            {
                return null; // TODO: warn user
            }

            var eis = new List<ProcessedEpisode>();
            foreach (var e in seas.Episodes)
            {
                eis.Add(new ProcessedEpisode(e, si)); // add a copy
            }

            if (si.DVDOrder)
            {
                eis.Sort(ProcessedEpisode.DVDOrderSorter);
                Renumber(eis);
            }
            else
            {
                eis.Sort(ProcessedEpisode.EPNumberSorter);
            }

            if (si.CountSpecials && seasonsToUse.ContainsKey(0))
            {
                // merge specials in
                foreach (var ep in seasonsToUse[0].Episodes)
                {
                    var seasstr = ep.AirsBeforeSeason;
                    var epstr = ep.AirsBeforeEpisode;
                    if (string.IsNullOrEmpty(seasstr) || string.IsNullOrEmpty(epstr))
                    {
                        continue;
                    }

                    var sease = int.Parse(seasstr);
                    if (sease != snum)
                    {
                        continue;
                    }

                    var epnum = int.Parse(epstr);
                    for (var i = 0; i < eis.Count; i++)
                    {
                        if (eis[i].AppropriateSeasonNumber != sease || eis[i].AppropriateEpNum != epnum)
                        {
                            continue;
                        }

                        var pe = new ProcessedEpisode(ep, si)
                        {
                            TheAiredSeason = eis[i].TheAiredSeason,
                            TheDVDSeason = eis[i].TheDVDSeason,
                            SeasonID = eis[i].SeasonID
                        };
                        eis.Insert(i, pe);
                        break;
                    }
                }

                // renumber to allow for specials
                var epnumr = 1;
                foreach (var t in eis)
                {
                    t.EpNum2 = epnumr + (t.EpNum2 - t.AppropriateEpNum);
                    t.AppropriateEpNum = epnumr;
                    epnumr++;
                }
            }

            if (!applyRules)
            {
                return eis;
            }

            var rules = si.RulesForSeason(snum);
            if (rules != null)
            {
                ApplyRules(eis, rules, si);
            }

            return eis;
        }

        public static void ApplyRules(List<ProcessedEpisode> eis, IEnumerable<SeasonRule> rules, ProcessedSeries si)
        {
            foreach (var sr in rules)
            {
                var nn1 = sr.First;
                var nn2 = sr.Second;
                var n1 = -1;
                var n2 = -1;

                // turn nn1 and nn2 from ep number into position in array
                for (var i = 0; i < eis.Count; i++)
                {
                    if (eis[i].AppropriateEpNum != nn1)
                    {
                        continue;
                    }
                    n1 = i;
                    break;
                }

                for (var i = 0; i < eis.Count; i++)
                {
                    if (eis[i].AppropriateEpNum != nn2)
                    {
                        continue;
                    }
                    n2 = i;
                    break;
                }

                if (sr.Action == RuleAction.Insert)
                {
                    // this only applies for inserting an episode, at the end of the list
                    if (nn1 == eis[eis.Count - 1].AppropriateEpNum + 1) // after the last episode
                    {
                        n1 = eis.Count;
                    }
                }

                var txt = sr.UserSuppliedText;
                var ec = eis.Count;

                switch (sr.Action)
                {
                    case RuleAction.Rename:
                        {
                            if (n1 < ec && n1 >= 0) eis[n1].Name = txt;
                            break;
                        }
                    case RuleAction.Remove:
                        {
                            if (n1 < ec && n1 >= 0 && n2 < ec && n2 >= 0)
                            {
                                eis.RemoveRange(n1, 1 + n2 - n1);
                            }
                            else if (n1 < ec && n1 >= 0 && n2 == -1)
                            {
                                eis.RemoveAt(n1);
                            }
                            break;
                        }
                    case RuleAction.IgnoreEp:
                        {
                            if (n2 == -1)
                            {
                                n2 = n1;
                            }
                            for (var i = n1; i <= n2; i++)
                            {
                                if (i < ec && i >= 0)
                                {
                                    eis[i].Ignore = true;
                                }
                            }
                            break;
                        }
                    case RuleAction.Split:
                        {
                            // split one episode into a multi-parter
                            if (n1 < ec && n1 >= 0)
                            {
                                var ei = eis[n1];
                                var nameBase = ei.Name;
                                eis.RemoveAt(n1); // remove old one
                                for (var i = 0; i < nn2; i++) // make n2 new parts
                                {
                                    var pe2 = new ProcessedEpisode(ei, si, ProcessedEpisode.ProcessedEpisodeType.split)
                                    {
                                        Name = nameBase + " (Part " + (i + 1) + ")",
                                        AiredEpNum = -2,
                                        DVDEpNum = -2,
                                        EpNum2 = -2
                                    };
                                    eis.Insert(n1 + i, pe2);
                                }
                            }

                            break;
                        }
                    case RuleAction.Merge:
                    case RuleAction.Collapse:
                        {
                            if (n1 != -1 && n2 != -1 && n1 < ec && n2 < ec && n1 < n2)
                            {
                                var oldFirstEi = eis[n1];
                                var episodeNames = new List<string> { eis[n1].Name };
                                var defaultCombinedName = eis[n1].Name + " + ";
                                var combinedSummary = eis[n1].Overview + "<br/><br/>";
                                var alleps = new List<TheTvDbEpisode> { eis[n1] };
                                for (var i = n1 + 1; i <= n2; i++)
                                {
                                    episodeNames.Add(eis[i].Name);
                                    defaultCombinedName += eis[i].Name;
                                    combinedSummary += eis[i].Overview;
                                    alleps.Add(eis[i]);
                                    if (i == n2)
                                    {
                                        continue;
                                    }
                                    defaultCombinedName += " + ";
                                    combinedSummary += "<br/><br/>";
                                }

                                eis.RemoveRange(n1, n2 - n1);
                                eis.RemoveAt(n1);

                                var combinedName = GetBestNameFor(episodeNames, defaultCombinedName);
                                var pe2 = new ProcessedEpisode(oldFirstEi, si, alleps)
                                {
                                    Name = string.IsNullOrEmpty(txt) ? combinedName : txt,
                                    AiredEpNum = -2,
                                    DVDEpNum = -2
                                };
                                if (sr.Action == RuleAction.Merge)
                                {
                                    pe2.EpNum2 = -2 + n2 - n1;
                                }
                                else
                                {
                                    pe2.EpNum2 = -2;
                                }

                                pe2.Overview = combinedSummary;
                                eis.Insert(n1, pe2);
                            }

                            break;
                        }
                    case RuleAction.Swap:
                        {
                            if (n1 != -1 && n2 != -1 && n1 < ec && n2 < ec)
                            {
                                var t = eis[n1];
                                eis[n1] = eis[n2];
                                eis[n2] = t;
                            }

                            break;
                        }
                    case RuleAction.Insert:
                        {
                            if (n1 < ec && n1 >= 0)
                            {
                                var t = eis[n1];
                                var n = new ProcessedEpisode(t.TheSeries, t.TheAiredSeason, t.TheDVDSeason, si)
                                {
                                    Name = txt,
                                    AiredEpNum = -2,
                                    DVDEpNum = -2,
                                    EpNum2 = -2
                                };
                                eis.Insert(n1, n);
                            }
                            else if (n1 == eis.Count)
                            {
                                var t = eis[n1 - 1];
                                var n = new ProcessedEpisode(t.TheSeries, t.TheAiredSeason, t.TheDVDSeason, si)
                                {
                                    Name = txt,
                                    AiredEpNum = -2,
                                    DVDEpNum = -2,
                                    EpNum2 = -2
                                };
                                eis.Add(n);
                            }

                            break;
                        }
                }

                Renumber(eis);
            }

            // now, go through and remove the ignored ones (but don't renumber!!)
            for (var i = eis.Count - 1; i >= 0; i--)
            {
                if (eis[i].Ignore)
                {
                    eis.RemoveAt(i);
                }
            }
        }

        public static string GetBestNameFor(List<string> episodeNames, string defaultName)
        {
            var root = StringHelper.GetCommonStartString(episodeNames);
            var shortestEpisodeName = episodeNames.Min(x => x.Length);
            var longestEpisodeName = episodeNames.Max(x => x.Length);
            var namesSameLength = shortestEpisodeName == longestEpisodeName;
            var rootIsIgnored = root.Trim().Equals("Episode", StringComparison.OrdinalIgnoreCase) ||
                                 root.Trim().Equals("Part", StringComparison.OrdinalIgnoreCase);

            if (!namesSameLength || rootIsIgnored || root.Length <= 3 || root.Length <= shortestEpisodeName / 2)
            {
                return defaultName;
            }

            char[] charsToTrim = { ',', '.', ';', ':', '-', '(' };
            string[] wordsToTrim = { "part", "episode" };

            return root.Trim().TrimEnd(wordsToTrim).Trim().TrimEnd(charsToTrim).Trim();
        }

        public static void Renumber(List<ProcessedEpisode> eis)
        {
            if (eis.Count == 0)
            {
                return; // nothing to do
            }

            // renumber 
            // pay attention to specials etc.
            var n = eis[0].AppropriateEpNum == 0 ? 0 : 1;
            foreach (var t in eis)
            {
                if (t.AppropriateEpNum == -1)
                {
                    continue;
                }

                var num = t.EpNum2 - t.AppropriateEpNum;
                t.AppropriateEpNum = n;
                t.EpNum2 = n + num;
                n += num + 1;
            }
        }

        public string GuessShowName(FolderMonitorEntry ai)
        {
            // see if we can guess a season number and show name, too
            // Assume is blah\blah\blah\show\season X
            var showName = ai.Folder;

            foreach (var seasonWord in GetSeasonWords())
            {
                var seasonFinder = ".*" + seasonWord + "[ _\\.]+([0-9]+).*";
                if (Regex.Matches(showName, seasonFinder, RegexOptions.IgnoreCase).Count == 0)
                {
                    continue;
                }

                try
                {
                    // remove season folder from end of the path
                    showName = Regex.Replace(showName, "(.*)\\\\" + seasonFinder, "$1", RegexOptions.IgnoreCase);
                    break;
                }
                catch (ArgumentException)
                {
                }
            }
            // assume last folder element is the show name
            showName = showName.Substring(showName.LastIndexOf(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) + 1);

            return showName;
        }

        public void WriteXmlSettings()
        {
            // backup old settings before writing new ones
            FileHelper.Rotate(FileSettings.TvDocSettingsFile.FullName);
            Logger.Info("Saving Settings to {0}", FileSettings.TvDocSettingsFile.FullName);

            var settings = new XmlWriterSettings
            {
                Indent = true,
                NewLineOnAttributes = true
            };

            using (var writer = XmlWriter.Create(FileSettings.TvDocSettingsFile.FullName, settings))
            {

                writer.WriteStartDocument();
                writer.WriteStartElement("TVRename");

                XmlHelper.WriteAttributeToXML(writer, "Version", "2.1");

                ApplicationSettings.Instance.WriteXML(writer); // <Settings>

                writer.WriteStartElement("MyShows");
                foreach (var si in _processedSeries)
                {
                    si.WriteXMLSettings(writer);
                }
                writer.WriteEndElement(); // myshows

                XmlHelper.WriteStringsToXml(_monitorFolders, writer, "MonitorFolders", "Folder");
                XmlHelper.WriteStringsToXml(_ignoreFolders, writer, "IgnoreFolders", "Folder");
                XmlHelper.WriteStringsToXml(SearchFolders, writer, "FinderSearchFolders", "Folder");

                writer.WriteStartElement("IgnoreItems");
                foreach (var ii in _ignoreItems)
                {
                    ii.Write(writer);
                }
                writer.WriteEndElement(); // IgnoreItems

                writer.WriteEndElement(); // tvrename
                writer.WriteEndDocument();
            }

            _dirty = false;

            GetStatistics().Save();
        }

        public bool LoadXmlSettings(FileInfo from)
        {
            Logger.Info("Loading Settings from {0}", from.FullName);

            try
            {
                var settings = new XmlReaderSettings
                {
                    IgnoreComments = true,
                    IgnoreWhitespace = true
                };

                if (!from.Exists)
                {
                    //LoadErr = from->Name + " : File does not exist";
                    //return false;
                    return true; // that's ok
                }

                using (var reader = XmlReader.Create(from.FullName, settings))
                {
                    reader.Read();
                    if (reader.Name != "xml")
                    {
                        _loadError = from.Name + " : Not a valid XML file";
                        return false;
                    }

                    reader.Read();
                    if (reader.Name != "TVRename")
                    {
                        _loadError = from.Name + " : Not a TVRename settings file";
                        return false;
                    }

                    if (reader.GetAttribute("Version") != "2.1")
                    {
                        _loadError = from.Name + " : Incompatible version";
                        return false;
                    }

                    reader.Read(); // move forward one
                    while (!reader.EOF)
                    {
                        if (reader.Name == "TVRename" && !reader.IsStartElement())
                        {
                            break; // end of it all
                        }

                        if (reader.Name == "Settings")
                        {
                            ApplicationSettings.Instance.Load(reader.ReadSubtree());
                            reader.Read();
                        }
                        else if (reader.Name == "MyShows")
                        {
                            var r2 = reader.ReadSubtree();
                            r2.Read();
                            r2.Read();
                            while (!r2.EOF)
                            {
                                if (r2.Name == "MyShows" && !r2.IsStartElement())
                                {
                                    break;
                                }
                                if (r2.Name == "ShowItem")
                                {
                                    var si = new ProcessedSeries(r2.ReadSubtree());

                                    if (si.UseCustomShowName) // see if custom show name is actually the real show name
                                    {
                                        var ser = si.TheSeries();
                                        if (ser != null && si.CustomShowName == ser.Name)
                                        {
                                            // then, turn it off
                                            si.CustomShowName = "";
                                            si.UseCustomShowName = false;
                                        }
                                    }

                                    _processedSeries.Add(si);

                                    r2.Read();
                                }
                                else
                                {
                                    r2.ReadOuterXml();
                                }
                            }

                            reader.Read();
                        }
                        else if (reader.Name == "MonitorFolders")
                        {
                            _monitorFolders = XmlHelper.ReadStringsFromXml(reader, "MonitorFolders", "Folder");
                        }
                        else if (reader.Name == "IgnoreFolders")
                        {
                            _ignoreFolders = XmlHelper.ReadStringsFromXml(reader, "IgnoreFolders", "Folder");
                        }
                        else if (reader.Name == "FinderSearchFolders")
                        {
                            SearchFolders = XmlHelper.ReadStringsFromXml(reader, "FinderSearchFolders", "Folder");
                        }
                        else if (reader.Name == "IgnoreItems")
                        {
                            var r2 = reader.ReadSubtree();
                            r2.Read();
                            r2.Read();
                            while (r2.Name == "Ignore")
                            {
                                _ignoreItems.Add(new IgnoreItem(r2));
                            }
                            reader.Read();
                        }
                        else
                        {
                            reader.ReadOuterXml();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Warn(e, "Problem on Startup loading File");
                _loadError = from.Name + " : " + e.Message;
                return false;
            }

            try
            {
                _statistics = Statistics.Load();
            }
            catch (Exception)
            {
                // not worried if stats loading fails
            }

            //Set these on the settings object so others can read them too - ideally should be refactored into the settings code
            ApplicationSettings.Instance.LibraryFoldersNames = _monitorFolders;
            ApplicationSettings.Instance.IgnoreFoldersNames = _ignoreFolders;
            ApplicationSettings.Instance.DownloadFoldersNames = SearchFolders;

            return true;
        }

        public void OutputActionXml(ApplicationSettings.ScanType st)
        {
            List<ActionListExporter> lup = new List<ActionListExporter> { new MissingXML(TheActionList), new MissingCSV(TheActionList), new CopyMoveXML(TheActionList), new RenamingXML(TheActionList) };

            foreach (ActionListExporter ue in lup)
            {
                if (ue.Active() && ue.ApplicableFor(st)) { ue.Run(); }
            }
        }

        public void ExportShowInfo()
        {
            ShowsTXT mx = new ShowsTXT(_processedSeries);
            mx.Run();
        }

        public List<ProcessedEpisode> NextNShows(int nShows, int nDaysPast, int nDaysFuture)
        {
            var notBefore = DateTime.Now.AddDays(-nDaysPast);
            var found = new List<ProcessedEpisode>();

            for (var i = 0; i < nShows; i++)
            {
                ProcessedEpisode nextAfterThat = null;
                var howClose = TimeSpan.MaxValue;
                foreach (var si in GetShowItems())
                {
                    if (!si.ShowNextAirdate)
                    {
                        continue;
                    }

                    foreach (var v in si.SeasonEpisodes)
                    {
                        if (si.IgnoreSeasons.Contains(v.Key))
                        {
                            continue; // ignore this season
                        }

                        foreach (var ei in v.Value)
                        {
                            if (found.Contains(ei))
                            {
                                continue;
                            }

                            var airdt = ei.GetAirDateDT(true);
                            if (airdt == null || airdt == DateTime.MaxValue)
                            {
                                continue;
                            }
                            var dt = (DateTime)airdt;

                            var ts = dt.Subtract(notBefore);
                            var timeUntil = dt.Subtract(DateTime.Now);
                            if (howClose != TimeSpan.MaxValue && (ts.CompareTo(howClose) > 0 || !(ts.TotalHours >= 0))
                                || !(ts.TotalHours >= 0) || !(timeUntil.TotalDays <= nDaysFuture))
                            {
                                continue;
                            }

                            howClose = ts;
                            nextAfterThat = ei;
                        }
                    }
                }

                if (nextAfterThat == null)
                {
                    return found;
                }

                var nextdt = nextAfterThat.GetAirDateDT(true);
                if (!nextdt.HasValue)
                {
                    continue;
                }
                notBefore = nextdt.Value;
                found.Add(nextAfterThat);
            }

            return found;
        }


        public void WriteUpcoming()
        {
            List<UpcomingExporter> lup = new List<UpcomingExporter> { new UpcomingRSS(this), new UpcomingXML(this) };

            foreach (UpcomingExporter ue in lup)
            {
                if (ue.Active()) { ue.Run(); }
            }
        }

        public void ProcessSingleAction(object infoIn)
        {
            try
            {
                if (!(infoIn is ProcessActionInfo info))
                {
                    return;
                }

                _actionSemaphores[info.SemaphoreNumber].WaitOne(); // don't start until we're allowed to
                _actionStarting = false; // let our creator know we're started ok

                var action = info.TheAction;
                if (action != null)
                {
                    Logger.Trace("Triggering Action: {0} - {1} - {2}", action.ActionName, action.ActionProduces, action.ToString());
                    action.PerformAction(ref _actionPause, _statistics);
                }

                _actionSemaphores[info.SemaphoreNumber].Release(1);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Unhandled Exception in Process Single Action");
            }
        }

        public ActionQueue[] ActionProcessorMakeQueues(ItemList theList)
        {
            // Take a single list
            // Return an array of "ActionQueue" items.
            // Each individual queue is processed sequentially, but all the queues run in parallel
            // The lists:
            //     - #0 all the cross filesystem moves, and all copies
            //     - #1 all quick "local" moves
            //     - #2 NFO Generator list
            //     - #3 Downloads (rss torrent, thumbnail, folder.jpg) across Settings.ParallelDownloads lists
            // We can discard any non-action items, as there is nothing to do for them

            var queues = new ActionQueue[4];
            queues[0] = new ActionQueue("Move/Copy", 1); // cross-filesystem moves (slow ones)
            queues[1] = new ActionQueue("Move/Delete", 1); // local rename/moves
            queues[2] = new ActionQueue("Write Metadata", 4); // writing KODI NFO files, etc.
            queues[3] = new ActionQueue("Download", ApplicationSettings.Instance.ParallelDownloads); // downloading torrents, banners, thumbnails

            foreach (var sli in theList)
            {
                if (!(sli is ActionBase action))
                {
                    continue; // skip non-actions
                }

                if (action is WriteMetaDataAction) // base interface that all metadata actions are derived from
                {
                    queues[2].ActionItems.Add(action);
                }
                else if (action is DownloadImageAction || action is RssAction)
                {
                    queues[3].ActionItems.Add(action);
                }
                else if (action is CopyMoveRenameFileAction fileAction)
                {
                    queues[fileAction.IsQuickOperation() ? 1 : 0].ActionItems.Add(fileAction);
                }
                else if (action is TouchFileAction) // add these after the slow copy operations
                {
                    queues[0].ActionItems.Add(action);
                }
                else if (action is DeleteFileAction || action is DeleteDirectoryAction)
                {
                    queues[1].ActionItems.Add(action);
                }
                else
                {
#if DEBUG
                    System.Diagnostics.Debug.Fail("Unknown action type for making processing queue");
#endif
                    Logger.Error("No action type found for {0}, Please follow up with a developer.", action.GetType());
                    queues[3].ActionItems.Add(action); // put it in this queue by default
                }
            }
            return queues;
        }

        private void ActionProcessor(object queuesIn)
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(queuesIn is ActionQueue[]);
#endif
            try
            {
                var queues = (ActionQueue[])queuesIn;
                var n = queues.Length;

                _actionWorkers = new List<Thread>();
                _actionSemaphores = new Semaphore[n];

                for (var i = 0; i < n; i++)
                {
                    _actionSemaphores[i] = new Semaphore(queues[i].ParallelLimit, queues[i].ParallelLimit); // allow up to numWorkers working at once
                    Logger.Info("Setting up '{0}' worker, with {1} threads in position {2}.", queues[i].Name, queues[i].ParallelLimit, i);
                }

                try
                {
                    for (; ; )
                    {
                        while (_actionPause)
                        {
                            Thread.Sleep(100);
                        }

                        // look through the list of semaphores to see if there is one waiting for some work to do
                        var allDone = true;
                        var which = -1;
                        for (var i = 0; i < n; i++)
                        {
                            // something to do in this queue, and semaphore is available
                            if (queues[i].ActionPosition >= queues[i].ActionItems.Count)
                            {
                                continue;
                            }

                            allDone = false;
                            if (!_actionSemaphores[i].WaitOne(20, false))
                            {
                                continue;
                            }

                            which = i;
                            break;
                        }

                        if (which == -1 && allDone)
                        {
                            break; // all done!
                        }

                        if (which == -1)
                        {
                            continue; // no semaphores available yet, try again for one
                        }

                        var queue = queues[which];
                        var act = queue.ActionItems[queue.ActionPosition++];

                        if (act == null)
                        {
                            continue;
                        }

                        if (!act.ActionCompleted)
                        {
                            var t = new Thread(ProcessSingleAction)
                            {
                                Name = "ProcessSingleAction(" + act.ActionName + ":" + act.ActionProgressText + ")"
                            };

                            _actionWorkers.Add(t);
                            _actionStarting = true; // set to false in thread after it has the semaphore
                            t.Start(new ProcessActionInfo(which, act));

                            var nfr = _actionSemaphores[which].Release(1); // release our hold on the semaphore, so that worker can grab it
                            Threadslogger.Trace("ActionProcessor[" + which + "] pool has " + nfr + " free");
                        }

                        while (_actionStarting) // wait for thread to get the semaphore
                        {
                            Thread.Sleep(10); // allow the other thread a chance to run and grab
                        }

                        // tidy up any finished workers
                        for (var i = _actionWorkers.Count - 1; i >= 0; i--)
                        {
                            if (!_actionWorkers[i].IsAlive)
                            {
                                _actionWorkers.RemoveAt(i); // remove dead worker
                            }
                        }
                    }

                    WaitForAllActionThreadsAndTidyUp();
                }
                catch (ThreadAbortException)
                {
                    foreach (var t in _actionWorkers)
                    {
                        t.Abort();
                    }
                    WaitForAllActionThreadsAndTidyUp();
                }
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Unhandled Exception in ActionProcessor");
                foreach (var t in _actionWorkers)
                {
                    t.Abort();
                }
                WaitForAllActionThreadsAndTidyUp();
            }
        }

        private void WaitForAllActionThreadsAndTidyUp()
        {
            if (_actionWorkers != null)
            {
                foreach (var t in _actionWorkers)
                {
                    if (t.IsAlive)
                    {
                        t.Join();
                    }
                }
            }

            _actionWorkers = null;
            _actionSemaphores = null;
        }

        public void DoActions(ItemList theList)
        {
            Logger.Info("**********************");
            Logger.Info("Doing Selected Actions....");
            if (theList == null)
            {
                return;
            }

            // Run tasks in parallel (as much as is sensible)
            var queues = ActionProcessorMakeQueues(theList);
            _actionPause = false;

            // If not /hide, show CopyMoveProgress dialog
            CopyMoveProgress cmp = null;
            if (!Args.Hide)
            {
                cmp = new CopyMoveProgress(this, queues);
            }

            _actionProcessorThread = new Thread(ActionProcessor)
            {
                Name = "ActionProcessorThread"
            };

            _actionProcessorThread.Start(queues);

            if (cmp != null && cmp.ShowDialog() == DialogResult.Cancel)
            {
                _actionProcessorThread.Abort();
            }

            _actionProcessorThread.Join();
            theList.RemoveAll(x => x is ActionBase @base && @base.ActionCompleted && !@base.ActionError);

            foreach (var sli in theList)
            {
                if (sli is ActionBase slia)
                {
                    Logger.Warn("Failed to complete the following action: {0}, doing {1}. Error was {2}", slia.ActionName, slia.ToString(), slia.ActionErrorText);
                }
            }

            Logger.Info("Completed Selected Actions");
            Logger.Info("**************************");

        }

        public static bool ListHasMissingItems(ItemList l)
        {
            foreach (var i in l)
            {
                if (i is MissingItem)
                {
                    return true;
                }
            }
            return false;
        }

        public void ActionGo(List<ProcessedSeries> shows)
        {
            _busy = true;
            if (ApplicationSettings.Instance.MissingCheck && !CheckAllFoldersExist(shows)) // only check for folders existing for missing check
            {
                return;
            }

            if (!DoDownloadsFg())
            {
                return;
            }

            var actionWork = new Thread(ScanWorker)
            {
                Name = "ActionWork"
            };

            _actionCancel = false;
            foreach (var f in _finders)
            {
                f.Reset();
            }

            if (!Args.Hide)
            {
                ScanProgDlg = new ScanProgress(ApplicationSettings.Instance.RenameCheck || ApplicationSettings.Instance.MissingCheck,
                    ApplicationSettings.Instance.MissingCheck && ApplicationSettings.Instance.SearchLocally,
                    ApplicationSettings.Instance.MissingCheck && (ApplicationSettings.Instance.CheckuTorrent || ApplicationSettings.Instance.CheckSABnzbd),
                    ApplicationSettings.Instance.MissingCheck && ApplicationSettings.Instance.SearchRSS);
            }
            else
                ScanProgDlg = null;

            actionWork.Start(shows);

            if ((ScanProgDlg != null) && (ScanProgDlg.ShowDialog() == DialogResult.Cancel))
            {
                _actionCancel = true;
                actionWork.Interrupt();
                foreach (FinderBase f in _finders)
                {
                    f.Interrupt();
                }
            }
            else
            {
                actionWork.Join();
            }

            ScanProgDlg = null;

            _downloadIdentifiers.Reset();

            _busy = false;
        }

        public void DoAllActions()
        {
            var theList = new ItemList();

            foreach (var action in _actionList)
            {
                if (action != null)
                {
                    theList.Add(action);
                }
            }

            DoActions(theList);
        }

        protected internal List<PossibleDuplicateEpisode> FindDoubleEps()
        {
            var output = new StringBuilder();
            var returnValue = new List<PossibleDuplicateEpisode>();

            output.AppendLine("");
            output.AppendLine("##################################################");
            output.AppendLine("DUPLICATE FINDER - Start");
            output.AppendLine("##################################################");

            var dfc = new DirectoryFileCache();
            foreach (var si in _processedSeries)
            {
                foreach (var kvp in si.SeasonEpisodes)
                {
                    //Ignore specials seasons
                    if (kvp.Key == 0)
                    {
                        continue;
                    }

                    //Ignore seasons that all aired on same date
                    var seasonMinAirDate = (from pep in kvp.Value select pep.FirstAired).Min();
                    var seasonMaxAirDate = (from pep in kvp.Value select pep.FirstAired).Max();
                    if (seasonMaxAirDate.HasValue && seasonMinAirDate.HasValue && seasonMaxAirDate == seasonMinAirDate)
                    {
                        continue;
                    }

                    //Search through each pair of episodes for the same season
                    foreach (var pep in kvp.Value)
                    {
                        if (pep.type == ProcessedEpisode.ProcessedEpisodeType.merged)
                        {
                            output.AppendLine(si.ShowName + " - Season: " + kvp.Key + " - " + pep.NumsAsString() + " - " + pep.Name + " is:");
                            foreach (var sourceEpisode in pep.sourceEpisodes)
                            {
                                output.AppendLine("                      - " + sourceEpisode.AiredEpNum + " - " + sourceEpisode.Name);
                            }
                        }

                        foreach (var comparePep in kvp.Value)
                        {
                            if (pep.FirstAired.HasValue && comparePep.FirstAired.HasValue && pep.FirstAired == comparePep.FirstAired && pep.EpisodeID < comparePep.EpisodeID)
                            {
                                // Tell user about this possibility
                                output.AppendLine($"{si.ShowName} - Season: {kvp.Key} - {pep.FirstAired.ToString()} - {pep.AiredEpNum}({pep.Name}) - {comparePep.AiredEpNum}({comparePep.Name})");

                                //do the 'name' test
                                var root = StringHelper.GetCommonStartString(pep.Name, comparePep.Name);
                                var sameLength = pep.Name.Length == comparePep.Name.Length;
                                var sameName = !root.Trim().Equals("Episode") && sameLength && root.Length > 3 && root.Length > pep.Name.Length / 2;
                                var oneFound = false;
                                var largerFileSize = false;

                                if (sameName)
                                {
                                    output.AppendLine("####### POSSIBLE DUPLICATE DUE TO NAME##########");

                                    //Do the missing Test (ie is one missing and not the other)
                                    var pepFound = FindEpOnDisk(dfc, pep).Count > 0;
                                    var comparePepFound = FindEpOnDisk(dfc, comparePep).Count > 0;
                                    oneFound = pepFound ^ comparePepFound;

                                    if (oneFound)
                                    {
                                        output.AppendLine("####### POSSIBLE DUPLICATE DUE TO ONE MISSING AND ONE FOUND ##########");

                                        var possibleDupEpisode = pepFound ? pep : comparePep;
                                        //Test the file sizes in the season
                                        //More than 40% longer
                                        var possibleDupFile = FindEpOnDisk(dfc, possibleDupEpisode)[0];
                                        var dupMovieLength = possibleDupFile.GetFilmLength();
                                        var otherMovieLengths = new List<int>();

                                        if (possibleDupFile.Directory != null)
                                        {
                                            otherMovieLengths.AddRange(
                                                from file in possibleDupFile.Directory.EnumerateFiles()
                                                where ApplicationSettings.Instance.UsefulExtension(file.Extension, false)
                                                select file.GetFilmLength());
                                        }

                                        var averageMovieLength = (otherMovieLengths.Sum() - dupMovieLength) / (otherMovieLengths.Count - 1);

                                        largerFileSize = dupMovieLength > averageMovieLength * 1.4;
                                        if (largerFileSize)
                                        {

                                            {
                                                output.AppendLine("######################################################################");
                                                output.AppendLine("####### SURELY WE HAVE ONE NOW                              ##########");
                                                output.AppendLine($"####### {possibleDupEpisode.AiredEpNum}({possibleDupEpisode.Name}) has length {dupMovieLength} greater than the average in the directory of {averageMovieLength}");
                                                output.AppendLine("######################################################################");
                                            }
                                        }
                                    }
                                }
                                returnValue.Add(new PossibleDuplicateEpisode(pep, comparePep, kvp.Key, true, sameName, oneFound, largerFileSize));
                            }
                        }
                    }
                }
            }
            output.AppendLine("##################################################");
            output.AppendLine("DUPLICATE FINDER - End");
            output.AppendLine("##################################################");

            Logger.Info(output.ToString());
            return returnValue;
        }


        public void QuickScan() => QuickScan(true, true);

        public void QuickScan(bool doMissingRecents, bool doFilesInDownloadDir)
        {
            _busy = true;

            var showsToScan = new List<ProcessedSeries>();
            if (doFilesInDownloadDir) showsToScan = GetShowsThatHaveDownloads();

            if (doMissingRecents)
            {
                var lpe = GetMissingEps();
                if (lpe != null)
                {
                    foreach (var pe in lpe)
                    {
                        if (!showsToScan.Contains(pe.SI)) showsToScan.Add(pe.SI);
                    }
                }
            }

            ActionGo(showsToScan);

            _busy = false;
        }

        public bool CheckAllFoldersExist(List<ProcessedSeries> showlist)
        {
            // show MissingFolderAction for any folders that are missing
            // return false if user cancels

            if (showlist == null) // nothing specified?
            {
                showlist = _processedSeries; // everything
            }

            foreach (var si in showlist)
            {
                if (!si.DoMissingCheck && !si.DoRename)
                {
                    continue; // skip
                }

                var flocs = si.AllFolderLocations();
                var numbers = new int[si.SeasonEpisodes.Keys.Count];
                si.SeasonEpisodes.Keys.CopyTo(numbers, 0);
                foreach (var snum in numbers)
                {
                    if (si.IgnoreSeasons.Contains(snum))
                    {
                        continue; // ignore this season
                    }

                    //int snum = kvp->Key;
                    if (snum == 0 && si.CountSpecials)
                    {
                        continue; // no specials season, they're merged into the seasons themselves
                    }

                    var folders = new List<string>();
                    if (flocs.ContainsKey(snum))
                    {
                        folders = flocs[snum];
                    }

                    if (folders.Count == 0 && !si.AutoAddNewSeasons)
                    {
                        continue; // no folders defined or found, autoadd off, so onto the next
                    }

                    if (folders.Count == 0)
                    {
                        // no folders defined for this season, and autoadd didn't find any, so suggest the autoadd folder name instead
                        folders.Add(si.AutoFolderNameForSeason(snum));
                    }

                    foreach (var folderFe in folders)
                    {
                        var folder = folderFe;

                        // generate new filename info
                        bool goAgain;
                        DirectoryInfo di = null;
                        var firstAttempt = true;

                        do
                        {
                            goAgain = false;
                            if (!string.IsNullOrEmpty(folder))
                            {
                                try
                                {
                                    di = new DirectoryInfo(folder);
                                }
                                catch
                                {
                                    break;
                                }
                            }

                            if (di == null || !di.Exists)
                            {
                                var sn = si.ShowName;
                                var text = snum + " of " + si.MaxSeason();
                                var theFolder = folder;
                                string otherFolder = null;

                                var whatToDo = FolderActionResult.NotSet;

                                if (Args.MissingFolder == CommandLineArgs.MissingFolderBehavior.Create)
                                {
                                    whatToDo = FolderActionResult.Create;
                                }
                                else if (Args.MissingFolder == CommandLineArgs.MissingFolderBehavior.Ignore)
                                {
                                    whatToDo = FolderActionResult.IgnoreOnce;
                                }

                                if (Args.Hide && (whatToDo == FolderActionResult.NotSet))
                                {
                                    whatToDo = FolderActionResult.IgnoreOnce; // default in /hide mode is to ignore
                                }

                                if (ApplicationSettings.Instance.AutoCreateFolders && firstAttempt)
                                {
                                    whatToDo = FolderActionResult.Create;
                                    firstAttempt = false;
                                }


                                if (whatToDo == FolderActionResult.NotSet)
                                {
                                    // no command line guidance, so ask the user
                                    // 									MissingFolderAction ^mfa = gcnew MissingFolderAction(sn, text, theFolder);
                                    // 									mfa->ShowDialog();
                                    // 									whatToDo = mfa->Result;
                                    // 									otherFolder = mfa->FolderName;

                                    MissingFolderAction mfa = new MissingFolderAction(sn, text, theFolder);
                                    mfa.ShowDialog();
                                    whatToDo = mfa.Result;
                                    otherFolder = mfa.FolderName;
                                }

                                if (whatToDo == FolderActionResult.Cancel)
                                {
                                    return false;
                                }
                                else if (whatToDo == FolderActionResult.Create)
                                {
                                    try
                                    {
                                        Logger.Info("Creating directory as it is missing: {0}", folder);
                                        Directory.CreateDirectory(folder);
                                    }
                                    catch (Exception ioe)
                                    {
                                        Logger.Info("Could not directory: {0}", folder);
                                        Logger.Info(ioe);
                                    }
                                    goAgain = true;
                                }
                                else if (whatToDo == FolderActionResult.IgnoreAlways)
                                {
                                    si.IgnoreSeasons.Add(snum);
                                    _dirty = true;
                                    break;
                                }
                                else if (whatToDo == FolderActionResult.IgnoreOnce)
                                {
                                    break;
                                }
                                else if (whatToDo == FolderActionResult.Retry)
                                {
                                    goAgain = true;
                                }
                                else if (whatToDo == FolderActionResult.DifferentFolder)
                                {
                                    folder = otherFolder;
                                    di = new DirectoryInfo(folder);
                                    goAgain = !di.Exists;
                                    if (di.Exists && !string.Equals(si.AutoFolderNameForSeason(snum), folder, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        if (!si.ManualFolderLocations.ContainsKey(snum))
                                        {
                                            si.ManualFolderLocations[snum] = new List<string>();
                                        }
                                        si.ManualFolderLocations[snum].Add(folder);
                                        _dirty = true;
                                    }
                                }
                            }
                        }
                        while (goAgain);
                    } // for each folder
                } // for each snum
            } // for each show

            return true;
        }

        public void RemoveIgnored()
        {
            var toRemove = new ItemList();
            foreach (var item in _actionList)
            {
                var act = item;
                foreach (var ii in _ignoreItems)
                {
                    if (!ii.SameFileAs(act.ItemIgnore))
                    {
                        continue;
                    }

                    toRemove.Add(item);
                    break;
                }
            }

            foreach (var action in toRemove)
            {
                _actionList.Remove(action);
            }
        }

        public void ForceUpdateImages(ProcessedSeries si)
        {

            _actionList = new ItemList();

            Logger.Info("*******************************");
            Logger.Info("Force Update Images: " + si.ShowName);

            if (!string.IsNullOrEmpty(si.AutoAdd_FolderBase) && si.AllFolderLocations().Count > 0)
            {
                _actionList.AddRange(_downloadIdentifiers.ForceUpdateShow(DownloadType.DownloadImage, si));
                si.BannersLastUpdatedOnDisk = DateTime.Now;
                _dirty = true;
            }

            // process each folder for each season...

            var numbers = new int[si.SeasonEpisodes.Keys.Count];
            si.SeasonEpisodes.Keys.CopyTo(numbers, 0);
            var allFolders = si.AllFolderLocations();

            foreach (var snum in numbers)
            {
                if (si.IgnoreSeasons.Contains(snum) || !allFolders.ContainsKey(snum))
                {
                    continue; // ignore/skip this season
                }

                if (snum == 0 && si.CountSpecials)
                {
                    continue; // don't process the specials season, as they're merged into the seasons themselves
                }

                // all the folders for this particular season
                var folders = allFolders[snum];

                foreach (var folder in folders)
                {
                    //Image series checks here
                    _actionList.AddRange(_downloadIdentifiers.ForceUpdateSeason(DownloadType.DownloadImage, si, folder, snum));
                }

            } // for each season of this show

            RemoveIgnored();
        }

        public void FindUnusedFilesInDlDirectory(List<ProcessedSeries> showList)
        {
            //for each directory in settings directory
            //for each file in directory
            //for each saved show (order by recent)
            //is file aready availabele? 
            //if so add show to list of files to be removed

            var dfc = new DirectoryFileCache();

            //When doing a fullscan the showlist is null indicating that all shows should be checked
            if (showList is null)
            {
                showList = _processedSeries;
            }

            foreach (var dirPath in SearchFolders)
            {
                if (!Directory.Exists(dirPath))
                {
                    continue;
                }

                foreach (var filePath in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
                {
                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    var fi = new FileInfo(filePath);
                    if (FileHelper.IgnoreFile(fi))
                    {
                        continue;
                    }

                    var matchingShows = new List<ProcessedSeries>();
                    foreach (var si in showList)
                    {
                        if (si.getSimplifiedPossibleShowNames().Any(name => FileHelper.SimplifyAndCheckFilename(fi.Name, name)))
                        {
                            matchingShows.Add(si);
                        }
                    }

                    if (matchingShows.Count > 0)
                    {
                        var fileCanBeRemoved = true;

                        foreach (var si in matchingShows)
                        {
                            if (FileNeeded(fi, si, dfc)) fileCanBeRemoved = false;
                        }

                        if (fileCanBeRemoved)
                        {
                            var si = matchingShows[0];//Choose the first series
                            FindSeasEp(fi, out var seasF, out var epF, out _, si, out _);
                            var s = si.TheSeries();
                            var ep = s.getEpisode(seasF, epF, si.DVDOrder);
                            var pep = new ProcessedEpisode(ep, si);
                            Logger.Info($"Removing {fi.FullName } as it matches {matchingShows[0].ShowName} and no episodes are needed");
                            _actionList.Add(new DeleteFileAction(fi, pep, ApplicationSettings.Instance.Tidyup));
                        }
                    }
                }

                foreach (var subDirPath in Directory.GetDirectories(dirPath, "*", SearchOption.AllDirectories))
                {
                    if (!Directory.Exists(subDirPath))
                    {
                        continue;
                    }

                    var di = new DirectoryInfo(subDirPath);
                    var matchingShows = new List<ProcessedSeries>();
                    foreach (var si in showList)
                    {
                        if (si.getSimplifiedPossibleShowNames().Any(name => FileHelper.SimplifyAndCheckFilename(di.Name, name)))
                        {
                            matchingShows.Add(si);
                        }
                    }

                    if (matchingShows.Count > 0)
                    {
                        var dirCanBeRemoved = true;
                        foreach (var si in matchingShows)
                        {
                            if (FileNeeded(di, si, dfc)) dirCanBeRemoved = false;
                        }

                        if (dirCanBeRemoved)
                        {
                            var si = matchingShows[0];//Choose the first series
                            FindSeasEp(di, out var seasF, out var epF, si, out _);
                            var s = si.TheSeries();
                            var ep = s.getEpisode(seasF, epF, si.DVDOrder);
                            var pep = new ProcessedEpisode(ep, si);
                            Logger.Info($"Removing {di.FullName } as it matches {matchingShows[0].ShowName} and no episodes are needed");
                            _actionList.Add(new DeleteDirectoryAction(di, pep, ApplicationSettings.Instance.Tidyup));
                        }
                    }
                }
            }
        }
        public bool FileNeeded(FileInfo fi, ProcessedSeries si, DirectoryFileCache dfc)
        {
            if (FindSeasEp(fi, out var seasF, out var epF, out _, si, out _))
            {
                var s = si.TheSeries();
                try
                {
                    var ep = s.getEpisode(seasF, epF, si.DVDOrder);
                    var pep = new ProcessedEpisode(ep, si);
                    if (FindEpOnDisk(dfc, si, pep).Count > 0)
                    {
                        return false;
                    }
                }
                catch (TheTvDbSeries.EpisodeNotFoundException)
                {
                    //Ignore execption, we may need the file
                    return true;
                }

            }
            //We may need the file
            return true;
        }

        public bool FileNeeded(DirectoryInfo di, ProcessedSeries si, DirectoryFileCache dfc)
        {
            if (FindSeasEp(di, out var seasF, out var epF, si, out _))
            {
                var s = si.TheSeries();
                try
                {
                    var ep = s.getEpisode(seasF, epF, si.DVDOrder);
                    var pep = new ProcessedEpisode(ep, si);

                    if (FindEpOnDisk(dfc, si, pep).Count > 0)
                    {
                        return false;
                    }
                }
                catch (TheTvDbSeries.EpisodeNotFoundException)
                {
                    //Ignore execption, we may need the file
                    return true;
                }

            }
            //We may need the file
            return true;
        }

        public void RenameAndMissingCheck(ProgressUpdatedDelegate prog, List<ProcessedSeries> showList)
        {
            _actionList = new ItemList();

            if (ApplicationSettings.Instance.RenameCheck)
            {
                GetStatistics().RenameChecksDone++;
            }

            if (ApplicationSettings.Instance.MissingCheck)
            {
                GetStatistics().MissingChecksDone++;
            }

            prog.Invoke(0);

            if (showList == null)
            {
                // only do episode count if we're doing all shows and seasons
                _statistics.NumberOfEpisodes = 0;
                showList = _processedSeries;
            }

            var dfc = new DirectoryFileCache();
            var c = 0;
            foreach (var si in showList)
            {
                if (_actionCancel)
                {
                    return;
                }

                Logger.Info("Rename and missing check: " + si.ShowName);
                c++;

                prog.Invoke(100 * c / showList.Count);

                if (si.AllFolderLocations().Count == 0) // no folders defined for this show
                {
                    continue; // so, nothing to do.
                }

                //This is the code that will iterate over the DownloadIdentifiers and ask each to ensure that
                //it has all the required files for that show
                if (!string.IsNullOrEmpty(si.AutoAdd_FolderBase) && (si.AllFolderLocations().Count > 0))
                {
                    _actionList.AddRange(_downloadIdentifiers.ProcessSeries(si));
                }

                //MS_TODO Put the bannerrefresh period into the settings file, we'll default to 3 months
                var cutOff = DateTime.Now.AddMonths(-3);
                var lastUpdate = si.BannersLastUpdatedOnDisk ?? DateTime.Now.AddMonths(-4);
                var timeForBannerUpdate = cutOff.CompareTo(lastUpdate) == 1;

                if (ApplicationSettings.Instance.NeedToDownloadBannerFile() && timeForBannerUpdate)
                {
                    _actionList.AddRange(_downloadIdentifiers.ForceUpdateShow(DownloadType.DownloadImage, si));
                    si.BannersLastUpdatedOnDisk = DateTime.Now;
                    _dirty = true;
                }

                // process each folder for each season...
                var numbers = new int[si.SeasonEpisodes.Keys.Count];
                si.SeasonEpisodes.Keys.CopyTo(numbers, 0);
                var allFolders = si.AllFolderLocations();
                var lastSeason = numbers.Concat(new[] { 0 }).Max();

                foreach (var snum in numbers)
                {
                    if (_actionCancel)
                    {
                        return;
                    }

                    if (si.IgnoreSeasons.Contains(snum) || !allFolders.ContainsKey(snum))
                    {
                        continue; // ignore/skip this season
                    }

                    if (snum == 0 && si.CountSpecials)
                    {
                        continue; // don't process the specials season, as they're merged into the seasons themselves
                    }

                    // all the folders for this particular season
                    var folders = allFolders[snum];
                    var folderNotDefined = folders.Count == 0;
                    if (folderNotDefined && ApplicationSettings.Instance.MissingCheck && !si.AutoAddNewSeasons)
                    {
                        continue; // folder for the season is not defined, and we're not auto-adding it
                    }

                    var eps = si.SeasonEpisodes[snum];
                    var maxEpisodeNumber = 0;
                    foreach (var episode in eps)
                    {
                        if (episode.AppropriateEpNum > maxEpisodeNumber)
                        {
                            maxEpisodeNumber = episode.AppropriateEpNum;
                        }
                    }

                    // base folder:
                    if (!string.IsNullOrEmpty(si.AutoAdd_FolderBase) && si.AllFolderLocations(false).Count > 0)
                    {
                        // main image for the folder itself
                        _actionList.AddRange(_downloadIdentifiers.ProcessSeries(si));
                    }

                    foreach (var folder in folders)
                    {
                        if (_actionCancel)
                        {
                            return;
                        }

                        var files = dfc.LoadCacheFromFolder(folder);
                        if (files == null)
                        {
                            continue;
                        }

                        if (ApplicationSettings.Instance.NeedToDownloadBannerFile() && timeForBannerUpdate)
                        {
                            //Image series checks here
                            _actionList.AddRange(_downloadIdentifiers.ForceUpdateSeason(DownloadType.DownloadImage, si, folder, snum));
                        }

                        var renCheck = ApplicationSettings.Instance.RenameCheck && si.DoRename && Directory.Exists(folder); // renaming check needs the folder to exist
                        var missCheck = ApplicationSettings.Instance.MissingCheck && si.DoMissingCheck;

                        //Image series checks here
                        _actionList.AddRange(_downloadIdentifiers.ProcessSeason(si, folder, snum));

                        var localEps = new FileInfo[maxEpisodeNumber + 1];

                        var maxEpNumFound = 0;
                        if (!renCheck && !missCheck)
                        {
                            continue;
                        }

                        foreach (var fi in files)
                        {
                            if (_actionCancel)
                            {
                                return;
                            }

                            if (!FindSeasEp(fi, out var seasNum, out var epNum, out _, si, out _))
                            {
                                continue; // can't find season & episode, so this file is of no interest to us
                            }

                            if (seasNum == -1)
                            {
                                seasNum = snum;
                            }

                            var epIdx = eps.FindIndex(x => x.AppropriateEpNum == epNum && x.AppropriateSeasonNumber == seasNum);
                            if (epIdx == -1)
                            {
                                continue; // season+episode number don't correspond to any episode we know of from thetvdb
                            }

                            var ep = eps[epIdx];
                            var actualFile = fi;

                            if (renCheck && ApplicationSettings.Instance.UsefulExtension(fi.Extension, true)) // == RENAMING CHECK ==
                            {
                                var newname = ApplicationSettings.Instance.FilenameFriendly(ApplicationSettings.Instance.NamingStyle.NameForExt(ep, fi.Extension, folder.Length));

                                if (newname != actualFile.Name)
                                {
                                    actualFile = FileHelper.FileInFolder(folder, newname); // rename updates the filename
                                    _actionList.Add(new CopyMoveRenameFileAction(fi, actualFile, FileOperationType.Rename, ep, null, null));

                                    //The following section informs the DownloadIdentifers that we already plan to
                                    //copy a file inthe appropriate place and they do not need to worry about downloading 
                                    //one for that purpse
                                    _downloadIdentifiers.NotifyComplete(actualFile);
                                }
                            }
                            if (missCheck && ApplicationSettings.Instance.UsefulExtension(fi.Extension, false)) // == MISSING CHECK part 1/2 ==
                            {
                                // first pass of missing check is to tally up the episodes we do have
                                localEps[epNum] = actualFile;
                                if (epNum > maxEpNumFound)
                                {
                                    maxEpNumFound = epNum;
                                }
                            }
                        } // foreach file in folder

                        if (missCheck) // == MISSING CHECK part 2/2 (includes NFO and Thumbnails) ==
                        {
                            // second part of missing check is to see what is missing!
                            // look at the offical list of episodes, and look to see if we have any gaps
                            var today = DateTime.Now;
                            TheTvDbClient.Instance.GetLock("UpToDateCheck");
                            foreach (var dbep in eps)
                            {
                                if (dbep.AppropriateEpNum > maxEpNumFound || localEps[dbep.AppropriateEpNum] == null) // not here locally
                                {
                                    var dt = dbep.GetAirDateDT(true);
                                    var dtOk = dt != null;
                                    var notFuture = dtOk && dt.Value.CompareTo(today) < 0; // isn't an episode yet to be aired
                                    var noAirdatesUntilNow = true;
                                    // for specials "season", see if any season has any airdates
                                    // otherwise, check only up to the season we are considering
                                    for (var i = 1; i <= (snum == 0 ? lastSeason : snum); i++)
                                    {
                                        if (HasAnyAirdates(si, i))
                                        {
                                            noAirdatesUntilNow = false;
                                            break;
                                        }
                                        else
                                        {//If the show is in its first season and no episodes have air dates
                                            if (lastSeason == 1)
                                            {
                                                noAirdatesUntilNow = false;
                                            }
                                        }
                                    }

                                    // only add to the missing list if, either:
                                    // - force check is on
                                    // - there are no airdates at all, for up to and including this season
                                    // - there is an airdate, and it isn't in the future
                                    if (noAirdatesUntilNow || (si.ForceCheckFuture || notFuture) && dtOk || si.ForceCheckNoAirdate && !dtOk)
                                    {
                                        // then add it as officially missing
                                        _actionList.Add(new MissingItem(dbep, folder,
                                            ApplicationSettings.Instance.FilenameFriendly(
                                                ApplicationSettings.Instance.NamingStyle.NameForExt(dbep, null,
                                                    folder.Length))));
                                    }
                                }
                                else
                                {
                                    // the file is here
                                    if (showList == null)
                                    {
                                        _statistics.NumberOfEpisodes++;
                                    }

                                    // do NFO and thumbnail checks if required
                                    var filo = localEps[dbep.AppropriateEpNum]; // filename (or future filename) of the file

                                    _actionList.AddRange(_downloadIdentifiers.ProcessEpisode(dbep, filo));

                                }
                            } // up to date check, for each episode in thetvdb
                            TheTvDbClient.Instance.Unlock("UpToDateCheck");
                        } // if doing missing check
                    } // for each folder for this season of this show
                } // for each season of this show
            } // for each show

            RemoveIgnored();
        }

        public void NoProgress(decimal pct)
        {
        }

        public void ScanWorker(object o)
        {
            try
            {
                var specific = (List<ProcessedSeries>)o;

                while (!Args.Hide && (ScanProgDlg == null || !ScanProgDlg.Ready))
                {
                    Thread.Sleep(10); // wait for thread to create the dialog
                }

                _actionList = new ItemList();
                ProgressUpdatedDelegate noProgress = NoProgress;

                if (ApplicationSettings.Instance.RenameCheck || ApplicationSettings.Instance.MissingCheck)
                {
                    RenameAndMissingCheck(ScanProgDlg == null ? noProgress : ScanProgDlg.MediaLibProg, specific);
                }

                if (ApplicationSettings.Instance.RemoveDownloadDirectoriesFiles)
                {
                    FindUnusedFilesInDlDirectory(specific);
                }

                if (ApplicationSettings.Instance.MissingCheck)
                {
                    // have a look around for any missing episodes
                    var activeLocalFinders = 0;
                    var activeRSSFinders = 0;
                    var activeDownloadingFinders = 0;

                    foreach (var f in _finders)
                    {
                        if (!f.Active()) continue;
                        f.ActionList = _actionList;

                        switch (f.DisplayType())
                        {
                            case FinderDisplayType.Local:
                                activeLocalFinders++;
                                break;
                            case FinderDisplayType.Downloading:
                                activeDownloadingFinders++;
                                break;
                            case FinderDisplayType.Rss:
                                activeRSSFinders++;
                                break;
                        }
                    }

                    var currentLocalFinderId = 0;
                    var currentRSSFinderId = 0;
                    var currentDownloadingFinderId = 0;

                    foreach (var f in _finders)
                    {
                        if (_actionCancel)
                        {
                            return;
                        }

                        if (f.Active() && ListHasMissingItems(_actionList))
                        {

                            int startPos;
                            int endPos;

                            switch (f.DisplayType())
                            {
                                case FinderDisplayType.Local:
                                    currentLocalFinderId++;
                                    startPos = 100 * (currentLocalFinderId - 1) / activeLocalFinders;
                                    endPos = 100 * currentLocalFinderId / activeLocalFinders;
                                    f.Check(ScanProgDlg == null ? noProgress : ScanProgDlg.LocalSearchProg, startPos, endPos);
                                    break;
                                case FinderDisplayType.Downloading:
                                    currentDownloadingFinderId++;
                                    startPos = 100 * (currentDownloadingFinderId - 1) / activeDownloadingFinders;
                                    endPos = 100 * currentDownloadingFinderId / activeDownloadingFinders;
                                    f.Check(ScanProgDlg == null ? noProgress : ScanProgDlg.DownloadingProg, startPos, endPos);
                                    break;
                                case FinderDisplayType.Rss:
                                    currentRSSFinderId++;
                                    startPos = 100 * (currentRSSFinderId - 1) / activeRSSFinders;
                                    endPos = 100 * currentRSSFinderId / activeRSSFinders;
                                    f.Check(ScanProgDlg == null ? noProgress : ScanProgDlg.RSSProg, startPos, endPos);
                                    break;
                            }

                            RemoveIgnored();
                        }
                    }
                }

                if (_actionCancel)
                {
                    return;
                }

                // sort Action list by type
                _actionList.Sort(new ItemSorter()); // was new ActionSorter()

                if (ScanProgDlg != null)
                {
                    ScanProgDlg.Done();
                }

                GetStatistics().FindAndOrganisesDone++;
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Unhandled Exception in ScanWorker");
            }
        }

        public static bool MatchesSequentialNumber(string filename, ref int seas, ref int ep, ProcessedEpisode pe)
        {
            if (pe.OverallNumber == -1)
            {
                return false;
            }

            var num = pe.OverallNumber.ToString();
            var found = Regex.Match("X" + filename + "X", "[^0-9]0*" + num + "[^0-9]").Success; // need to pad to let it match non-numbers at start and end
            if (found)
            {
                seas = pe.AppropriateSeasonNumber;
                ep = pe.AppropriateEpNum;
            }
            return found;
        }

        public static string SeFinderSimplifyFilename(string filename, string showNameHint)
        {
            // Look at showNameHint and try to remove the first occurance of it from filename
            // This is very helpful if the showname has a >= 4 digit number in it, as that
            // would trigger the 1302 -> 13,02 matcher
            // Also, shows like "24" can cause confusion


            //TODO: More replacement of non useful characters - MarkSummerville
            filename = filename.Replace(".", " "); // turn dots into spaces

            if (string.IsNullOrEmpty(showNameHint))
            {
                return filename;
            }

            var nameIsNumber = Regex.Match(showNameHint, "^[0-9]+$").Success;
            var p = filename.IndexOf(showNameHint, StringComparison.Ordinal);

            if (p == 0)
            {
                filename = filename.Remove(0, showNameHint.Length);
                return filename;
            }

            if (nameIsNumber) // e.g. "24", or easy exact match of show name at start of filename
            {
                filename = filename.Remove(0, showNameHint.Length);
                return filename;
            }

            foreach (Match m in Regex.Matches(showNameHint, "(?:^|[^a-z]|\\b)([0-9]{3,})")) // find >= 3 digit numbers in show name
            {
                if (m.Groups.Count > 1) // just in case
                {
                    var number = m.Groups[1].Value;
                    filename = Regex.Replace(filename, "(^|\\W)" + number + "\\b", ""); // remove any occurances of that number in the filename
                }
            }

            return filename;
        }

        private static bool FindSeasEpDateCheck(FileInfo fi, out int seas, out int ep, out int maxEp, ProcessedSeries si)
        {
            if (fi == null || si == null)
            {
                seas = -1;
                ep = -1;
                maxEp = -1;
                return false;
            }

            // look for a valid airdate in the filename
            // check for YMD, DMY, and MDY
            // only check against airdates we expect for the given show
            var ser = TheTvDbClient.Instance.GetSeries(si.TVDBCode);
            var dateFormats = new[] { "yyyy-MM-dd", "dd-MM-yyyy", "MM-dd-yyyy", "yy-MM-dd", "dd-MM-yy", "MM-dd-yy" };
            var filename = fi.Name;
            // force possible date separators to a dash
            filename = filename.Replace("/", "-");
            filename = filename.Replace(".", "-");
            filename = filename.Replace(",", "-");
            filename = filename.Replace(" ", "-");

            ep = -1;
            seas = -1;
            maxEp = -1;
            var seasonsToUse = si.DVDOrder ? ser.DVDSeasons : ser.AiredSeasons;

            foreach (var kvp in seasonsToUse)
            {
                if (si.IgnoreSeasons.Contains(kvp.Value.SeasonNumber))
                {
                    continue;
                }

                foreach (var epi in kvp.Value.Episodes)
                {
                    var dt = epi.GetAirDateDT(); // file will have local timezone date, not ours
                    if (dt == null)
                    {
                        continue;
                    }

                    var closestDate = TimeSpan.MaxValue;
                    foreach (var dateFormat in dateFormats)
                    {
                        var datestr = dt.Value.ToString(dateFormat);
                        if (filename.Contains(datestr) && DateTime.TryParseExact(datestr, dateFormat, new CultureInfo("en-GB"), DateTimeStyles.None, out var dtInFilename))
                        {
                            var timeAgo = DateTime.Now.Subtract(dtInFilename);
                            if (timeAgo < closestDate)
                            {
                                seas = si.DVDOrder ? epi.DVDSeasonNumber : epi.AiredSeasonNumber;
                                ep = si.DVDOrder ? epi.DVDEpNum : epi.AiredEpNum;
                                closestDate = timeAgo;
                            }
                        }
                    }
                }
            }

            return ep != -1 && seas != -1;
        }

        public List<ProcessedEpisode> GetMissingEps()
        {
            var dd = ApplicationSettings.Instance.WTWRecentDays;
            var dfc = new DirectoryFileCache();
            return GetMissingEps(dfc, GetRecentAndFutureEps(dfc, dd));
        }


        public List<ProcessedEpisode> GetRecentAndFutureEps(DirectoryFileCache dfc, int days)
        {
            var returnList = new List<ProcessedEpisode>();

            foreach (var si in GetShowItems())
            {
                if (!si.ShowNextAirdate)
                {
                    continue;
                }

                foreach (var kvp in si.SeasonEpisodes)
                {
                    if (si.IgnoreSeasons.Contains(kvp.Key))
                    {
                        continue; // ignore this season
                    }

                    var eis = kvp.Value;
                    var nextToAirFound = false;

                    foreach (var ei in eis)
                    {
                        var dt = ei.GetAirDateDT(true);
                        if (dt != null && dt.Value.CompareTo(DateTime.MaxValue) != 0)
                        {
                            var ts = dt.Value.Subtract(DateTime.Now);
                            if (ts.TotalHours >= (-24 * days)) // in the future (or fairly recent)
                            {
                                if (ts.TotalHours >= 0 && !nextToAirFound)
                                {
                                    nextToAirFound = true;
                                    ei.NextToAir = true;
                                }
                                else
                                {
                                    ei.NextToAir = false;
                                }
                                returnList.Add(ei);
                            }
                        }
                    }
                }
            }

            return returnList;
        }

        public static List<ProcessedEpisode> GetMissingEps(DirectoryFileCache dfc, IEnumerable<ProcessedEpisode> lpe)
        {
            var missing = new List<ProcessedEpisode>();

            foreach (var pe in lpe)
            {
                var fl = FindEpOnDisk(dfc, pe);

                var foundOnDisk = (fl != null && (fl.Count > 0));
                var alreadyAired = pe.GetAirDateDT(true).Value.CompareTo(DateTime.Now) < 0;

                if (!foundOnDisk && alreadyAired && pe.SI.DoMissingCheck)
                {
                    missing.Add(pe);
                }
            }
            return missing;
        }

        private List<ProcessedSeries> GetShowsThatHaveDownloads()
        {
            //for each directory in settings directory
            //for each file in directory
            //for each saved show (order by recent)
            //does show match selected file?
            //if so add series to list of series scanned
            var showsToScan = new List<ProcessedSeries>();
            foreach (var dirPath in SearchFolders)
            {
                if (!Directory.Exists(dirPath))
                {
                    continue;
                }

                foreach (var filePath in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
                {
                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    var fi = new FileInfo(filePath);
                    if (FileHelper.IgnoreFile(fi))
                    {
                        continue;
                    }

                    foreach (var si in _processedSeries)
                    {
                        if (showsToScan.Contains(si))
                        {
                            continue;
                        }

                        if (si.getSimplifiedPossibleShowNames().Any(name => FileHelper.SimplifyAndCheckFilename(fi.Name, name)))
                        {
                            showsToScan.Add(si);
                        }
                    }
                }

                foreach (var subDirPath in Directory.GetDirectories(dirPath, "*", SearchOption.AllDirectories))
                {
                    if (!Directory.Exists(subDirPath))
                    {
                        continue;
                    }

                    var di = new DirectoryInfo(subDirPath);
                    foreach (var si in _processedSeries)
                    {
                        if (showsToScan.Contains(si))
                        {
                            continue;
                        }

                        if (si.getSimplifiedPossibleShowNames().Any(name => FileHelper.SimplifyAndCheckFilename(di.Name, name)))
                        {
                            showsToScan.Add(si);
                        }
                    }

                }
            }

            return showsToScan;
        }

        internal void ForceRefresh(List<ProcessedSeries> sis)
        {
            if (sis != null)
            {
                foreach (var si in sis)
                {
                    TheTvDbClient.Instance.ForgetShow(si.TVDBCode, true);
                }
            }
            DoDownloadsFg();
        }

        public static bool FindSeasEp(FileInfo fi, out int seas, out int ep, out int maxEp, ProcessedSeries si, out FilenameProcessorRegEx re)
        {
            return FindSeasEp(fi, out seas, out ep, out maxEp, si, ApplicationSettings.Instance.FNPRegexs,
                ApplicationSettings.Instance.LookForDateInFilename, out re);
        }

        public static bool FindSeasEp(FileInfo fi, out int seas, out int ep, out int maxEp, ProcessedSeries si, IEnumerable<FilenameProcessorRegEx> rexps, bool doDateCheck, out FilenameProcessorRegEx re)
        {
            re = null;
            if (fi == null)
            {
                seas = -1;
                ep = -1;
                maxEp = -1;
                return false;
            }

            if (doDateCheck && FindSeasEpDateCheck(fi, out seas, out ep, out maxEp, si))
            {
                return true;
            }

            var filename = fi.Name;
            var l = filename.Length;
            var le = fi.Extension.Length;
            filename = filename.Substring(0, l - le);
            return FindSeasEp(fi.Directory?.FullName, filename, out seas, out ep, out maxEp, si, rexps, out re);
        }

        public static bool FindSeasEp(DirectoryInfo di, out int seas, out int ep, ProcessedSeries si, out FilenameProcessorRegEx re)
        {
            var rexps = ApplicationSettings.Instance.FNPRegexs;
            re = null;

            if (di == null)
            {
                seas = -1;
                ep = -1;
                return false;
            }

            return FindSeasEp(di.Parent?.FullName, di.Name, out seas, out ep, out _, si, rexps, out re);
        }


        public static bool FindSeasEp(string directory, string filename, out int seas, out int ep, out int maxEp, ProcessedSeries si, IEnumerable<FilenameProcessorRegEx> rexps)
        {
            return FindSeasEp(directory, filename, out seas, out ep, out maxEp, si, rexps, out _);
        }


        public static bool FindSeasEp(string directory, string filename, out int seas, out int ep, out int maxEp, ProcessedSeries si, IEnumerable<FilenameProcessorRegEx> rexps, out FilenameProcessorRegEx rex)
        {
            var showNameHint = si != null ? si.ShowName : "";
            maxEp = -1;
            seas = ep = -1;
            rex = null;

            filename = SeFinderSimplifyFilename(filename, showNameHint);
            var fullPath = directory + Path.DirectorySeparatorChar + filename; // construct full path with sanitised filename

            filename = filename.ToLower() + " ";
            fullPath = fullPath.ToLower() + " ";

            foreach (var re in rexps)
            {
                if (!re.Enabled)
                {
                    continue;
                }

                try
                {
                    var m = Regex.Match(re.UseFullPath ? fullPath : filename, re.RegEx, RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        if (!int.TryParse(m.Groups["s"].ToString(), out seas))
                        {
                            seas = -1;
                        }

                        if (!int.TryParse(m.Groups["e"].ToString(), out ep))
                        {
                            ep = -1;
                        }

                        if (!int.TryParse(m.Groups["f"].ToString(), out maxEp))
                        {
                            maxEp = -1;
                        }

                        rex = re;
                        if (seas != -1 || ep != -1) return true;
                    }
                }
                catch (FormatException)
                {
                }
                catch (ArgumentException)
                {
                }
            }

            return seas != -1 || ep != -1;
        }

        #region Nested type: ProcessActionInfo

        private class ProcessActionInfo
        {
            public readonly int SemaphoreNumber;
            public readonly ActionBase TheAction;

            public ProcessActionInfo(int n, ActionBase a)
            {
                SemaphoreNumber = n;
                TheAction = a;
            }
        };

        #endregion

        private ProcessedSeries ShowItemForCode(int code)
        {
            foreach (var si in _processedSeries)
            {
                if (si.TVDBCode == code)
                {
                    return si;
                }
            }
            return null;
        }

        internal List<ProcessedSeries> GetRecentShows()
        {
            // only scan "recent" shows
            var shows = new List<ProcessedSeries>();
            var dd = ApplicationSettings.Instance.WTWRecentDays;

            // for each show, see if any episodes were aired in "recent" days...
            foreach (var si in GetShowItems())
            {
                var added = false;

                foreach (var kvp in si.SeasonEpisodes)
                {
                    if (added)
                    {
                        break;
                    }

                    if (si.IgnoreSeasons.Contains(kvp.Key))
                    {
                        continue; // ignore this season
                    }

                    var eis = kvp.Value;
                    foreach (var ei in eis)
                    {
                        var dt = ei.GetAirDateDT(true);
                        if (dt != null && dt.Value.CompareTo(DateTime.MaxValue) != 0)
                        {
                            var ts = dt.Value.Subtract(DateTime.Now);
                            if (ts.TotalHours >= -24 * dd && ts.TotalHours <= 0) // fairly recent?
                            {
                                shows.Add(si);
                                added = true;
                                break;
                            }
                        }
                    }
                }
            }

            return shows;
        }

        public UpdateVersion CheckForUpdates()
        {
            const string githubReleasesApiUrl = "https://api.github.com/repos/TV-Rename/tvrename/releases";
            UpdateVersion currentVersion;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                var currentVersionString = SystemHelper.GetDisplayVersion;
                var inDebug = currentVersionString.EndsWith(" ** Debug Build **");

                //remove debug stuff
                if (inDebug)
                {
                    currentVersionString = currentVersionString.Substring(0, currentVersionString.LastIndexOf(" ** Debug Build **", StringComparison.Ordinal));
                }

                currentVersion = new UpdateVersion(currentVersionString, UpdateVersion.VersionType.Friendly);
            }
            catch (ArgumentException e)
            {
                Logger.Error("Failed to establish if there are any new releases as could not parse internal version: " + SystemHelper.GetDisplayVersion, e);
                return null;
            }

            UpdateVersion latestVersion = null;
            UpdateVersion latestBetaVersion = null;

            try
            {
                var client = new WebClient();
                client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
                var response = client.DownloadString(githubReleasesApiUrl);
                var gitHubInfo = JArray.Parse(response);

                foreach (var gitHubReleaseJson in gitHubInfo.Children<JObject>())
                {
                    try
                    {
                        DateTime.TryParse(gitHubReleaseJson["published_at"].ToString(), out var releaseDate);
                        var testVersion = new UpdateVersion(gitHubReleaseJson["tag_name"].ToString(), UpdateVersion.VersionType.Semantic)
                        {
                            DownloadUrl = gitHubReleaseJson["assets"][0]["browser_download_url"].ToString(),
                            ReleaseNotesText = gitHubReleaseJson["body"].ToString(),
                            ReleaseNotesUrl = gitHubReleaseJson["html_url"].ToString(),
                            ReleaseDate = releaseDate,
                            IsBeta = (gitHubReleaseJson["prerelease"].ToString() == "True")
                        };

                        //all versions want to be considered if you are in the beta stream
                        if (testVersion.NewerThan(latestBetaVersion)) latestBetaVersion = testVersion;

                        //If the latest version is a production one then update the latest production version
                        if (!testVersion.IsBeta)
                        {
                            if (testVersion.NewerThan(latestVersion)) latestVersion = testVersion;
                        }
                    }
                    catch (NullReferenceException ex)
                    {
                        Logger.Warn("Looks like the JSON payload from GitHub has changed");
                        Logger.Debug(ex, gitHubReleaseJson.ToString());
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        Logger.Debug("Generally happens because the release did not have an exe attached");
                        Logger.Debug(ex, gitHubReleaseJson.ToString());
                    }

                }
                if (latestVersion == null)
                {
                    Logger.Error("Could not find latest version information from GitHub: {0}", response);
                    return null;
                }

                if (latestBetaVersion == null)
                {
                    Logger.Error("Could not find latest beta version information from GitHub: {0}", response);
                    return null;
                }

            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to contact GitHub to identify new releases");
                return null;

            }

            if (ApplicationSettings.Instance.Mode == ApplicationSettings.BetaMode.ProductionOnly && latestVersion.NewerThan(currentVersion))
            {
                return latestVersion;
            }

            if (ApplicationSettings.Instance.Mode == ApplicationSettings.BetaMode.BetaToo && latestBetaVersion.NewerThan(currentVersion))
            {
                return latestBetaVersion;
            }

            return null;
        }

        private void ReleaseUnmanagedResources()
        {
            StopBgDownloadThread();
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();

            if (disposing)
            {
                // ReSharper disable once UseNullPropagation
                // ReSharper disable once UseNullPropagation
                if (_workerSemaphore != null) _workerSemaphore.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
