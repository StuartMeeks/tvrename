using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using NLog;
using TVRename.AppLogic.Delegates;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.ScanItems.Items;
using TVRename.AppLogic.Settings;

namespace TVRename.AppLogic.BitTorrent
{
    public class BtResume : BtCore
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool Altered;
        public bool DoMatchMissing;
        public bool HashSearch;
        public ItemList MissingList;

        public string NewLocation;
        public bool PrioWasSet;

        public BtFile ResumeDat; // resume file, if we're using it
        public string ResumeDatPath;

        public List<FilenameProcessorRegEx> Rexps; // used by MatchMissing
        public bool SearchSubFolders;
        public bool SetPrios;
        public bool TestMode;
        public string Type;

        public BtResume(ProgressUpdatedDelegate setprog, string resumeDatFile)
            : base(setprog)
        {
            ResumeDatPath = resumeDatFile;
        }

        public BtDictionary GetTorrentDict(string torrentFile)
        {
            // find dictionary for the specified torrent file
            var it = ResumeDat.GetDict().GetItem(torrentFile, true);
            if (it == null || it.Type != BtChunk.Dictionary) return null;

            var dict = (BtDictionary) it;
            return dict;
        }

        public static int PercentBitsOn(BtString s)
        {
            var totalBits = 0;
            var bitsOn = 0;

            for (var i = 0; i < s.Data.Length; i++)
            {
                totalBits += 8;
                var c = s.Data[i];
                for (var j = 0; j < 8; j++)
                {
                    if ((c & 0x01) != 0) bitsOn++;
                    c >>= 1;
                }
            }

            return (100 * bitsOn + totalBits / 2) / totalBits;
        }

        public List<TorrentEntry> AllFilesBeingDownloaded()
        {
            var r = new List<TorrentEntry>();

            var bel = new BtEncodeLoader();
            foreach (var it in ResumeDat.GetDict().Items)
            {
                if (it.Type != BtChunk.DictionaryItem) continue;

                var dictitem = it;

                if (dictitem.Key == ".fileguard" || dictitem.Data.Type != BtChunk.Dictionary) continue;

                var torrentFile = dictitem.Key;
                var d2 = (BtDictionary) dictitem.Data;
                var p = d2.GetItem("prio");
                if (p == null || p.Type != BtChunk.String) continue;

                var prioString = (BtString) p;
                var directoryName = Path.GetDirectoryName(ResumeDatPath) + Path.DirectorySeparatorChar;

                if (!File.Exists(torrentFile)) // if the torrent file doesn't exist
                    torrentFile = directoryName + torrentFile; // ..try prepending the resume.dat folder's path to it.

                if (!File.Exists(torrentFile)) continue; // can't find it.  give up!

                var tor = bel.Load(torrentFile);

                var a = tor?.AllFilesInTorrent();
                if (a != null)
                {
                    var c = 0;

                    p = d2.GetItem("path");
                    if (p == null || p.Type != BtChunk.String) continue;

                    var defaultFolder = ((BtString) p).AsString();
                    var targets = d2.GetItem("targets");
                    var hasTargets = targets != null && targets.Type == BtChunk.List;
                    var targetList = (BtList) targets;

                    foreach (var s in a)
                    {
                        if (c < prioString.Data.Length && prioString.Data[c] != BTPrio.Skip)
                            try
                            {
                                var saveTo = FileHelper.FileInFolder(defaultFolder,
                                    ApplicationSettings.Instance.FilenameFriendly(s)).Name;
                                if (hasTargets)
                                    for (var i = 0; i < targetList.Items.Count; i++)
                                    {
                                        var l = (BtList) targetList.Items[i];
                                        var n = (BtInteger) l.Items[0];
                                        var dest = (BtString) l.Items[1];
                                        if (n.Value == c)
                                        {
                                            saveTo = dest.AsString();
                                            break;
                                        }
                                    }

                                var percent = a.Count == 1 ? PercentBitsOn((BtString) d2.GetItem("have")) : -1;
                                var te = new TorrentEntry(torrentFile, saveTo, percent);
                                r.Add(te);
                            }
                            catch (PathTooLongException ptle)
                            {
                                //this is not the file we are looking for
                                logger.Debug(ptle);
                            }

                        c++;
                    }
                }
            }

            return r;
        }

        public string GetResumePrio(string torrentFile, int fileNum)
        {
            var dict = GetTorrentDict(torrentFile);
            if (dict == null)
                return "";
            var p = dict.GetItem("prio");
            if (p == null || p.Type != BtChunk.String)
                return "";
            var prioString = (BtString) p;
            if (fileNum < 0 || fileNum > prioString.Data.Length)
                return "";

            int pr = prioString.Data[fileNum];
            if (pr == BTPrio.Normal)
                return "Normal";
            if (pr == BTPrio.Skip)
                return "Skip";
            return pr.ToString();
        }

        public void SetResumePrio(string torrentFile, int fileNum, byte newPrio)
        {
            if (!SetPrios)
                return;

            if (fileNum == -1)
                fileNum = 0;
            var dict = GetTorrentDict(torrentFile);
            if (dict == null)
                return;
            var p = dict.GetItem("prio");
            if (p == null || p.Type != BtChunk.String)
                return;
            var prioString = (BtString) p;
            if (fileNum < 0 || fileNum > prioString.Data.Length)
                return;

            Altered = true;
            PrioWasSet = true;

            prioString.Data[fileNum] = newPrio;

            string ps;
            if (newPrio == BTPrio.Skip)
                ps = "Skip";
            else if (newPrio == BTPrio.Normal)
                ps = "Normal";
            else
                ps = newPrio.ToString();
        }

        public void AlterResume(string torrentFile, int fileNum, string toHere)
        {
            toHere = RemoveUT(toHere);

            var dict = GetTorrentDict(torrentFile);
            if (dict == null)
                return;

            Altered = true;

            if (fileNum == -1) // single file torrent
            {
                var p = dict.GetItem("path");
                if (p == null)
                {
                    dict.Items.Add(new BtDictionaryItem("path", new BtString(toHere)));
                }
                else
                {
                    if (p.Type != BtChunk.String)
                        return;
                    ((BtString) p).SetString(toHere);
                }
            }
            else
            {
                // multiple file torrent, uses a list called "targets"
                var p = dict.GetItem("targets");
                BtList theList = null;
                if (p == null)
                {
                    theList = new BtList();
                    dict.Items.Add(new BtDictionaryItem("targets", theList));
                }
                else
                {
                    if (p.Type != BtChunk.List)
                        return;
                    theList = (BtList) p;
                }

                if (theList == null)
                    return;

                // the list contains two element lists, of integer/string which are filenumber/path

                BtList thisFileList = null;
                // see if this file is already in the list
                for (var i = 0; i < theList.Items.Count; i++)
                {
                    if (theList.Items[i].Type != BtChunk.List)
                        return;

                    var l2 = (BtList) theList.Items[i];
                    if (l2.Items.Count != 2 || l2.Items[0].Type != BtChunk.Integer ||
                        l2.Items[1].Type != BtChunk.String)
                        return;
                    var n = (int) ((BtInteger) l2.Items[0]).Value;
                    if (n == fileNum)
                    {
                        thisFileList = l2;
                        break;
                    }
                }

                if (thisFileList == null) // didn't find it
                {
                    thisFileList = new BtList();
                    thisFileList.Items.Add(new BtInteger(fileNum));
                    thisFileList.Items.Add(new BtString(toHere));
                    theList.Items.Add(thisFileList);
                }
                else
                {
                    thisFileList.Items[1] = new BtString(toHere);
                }
            }
        }

        public void FixFileguard()
        {
            // finally, fix up ".fileguard"
            // this is the SHA1 of the entire file, without the .fileguard
            ResumeDat.GetDict().RemoveItem(".fileguard");
            var ms = new MemoryStream();
            ResumeDat.Write(ms);
            var sha1 = new SHA1Managed();
            var theHash = sha1.ComputeHash(ms.GetBuffer(), 0, (int) ms.Length);
            ms.Close();
            var newfg = BtString.CharsToHex(theHash, 0, 20);
            ResumeDat.GetDict().Items.Add(new BtDictionaryItem(".fileguard", new BtString(newfg)));
        }

        public FileInfo MatchMissing(string torrentFile, int torrentFileNum, string nameInTorrent)
        {
            // returns true if we found a match (if actSetPrio is on, true also means we have set a priority for this file)
            var simplifiedfname = FileHelper.SimplifyName(nameInTorrent);

            foreach (var Action1 in MissingList)
            {
                if (!(Action1 is MissingItem) && !(Action1 is UTorrentingItem) && !(Action1 is SabNzbDItem))
                    continue;

                ProcessedEpisode m = null;
                string name = null;

                if (Action1 is MissingItem)
                {
                    var Action = (MissingItem) Action1;
                    m = Action.ItemEpisode;
                    name = Action.TheFileNoExt;
                }
                else if (Action1 is UTorrentingItem)
                {
                    var Action = (UTorrentingItem) Action1;
                    m = Action.ItemEpisode;
                    name = Action.DesiredLocationNoExt;
                }
                else if (Action1 is SabNzbDItem)
                {
                    var Action = (SabNzbDItem) Action1;
                    m = Action.ItemEpisode;
                    name = Action.DesiredLocationNoExt;
                }

                if (m == null || string.IsNullOrEmpty(name))
                    continue;

                // see if the show name matches...
                if (FileHelper.SimplifyAndCheckFilename(simplifiedfname, m.TheSeries.Name, false, true))
                {
                    // see if season and episode match
                    bool findFile = TvRenameManager.FindSeasEp("", simplifiedfname, out int seasF, out int epF, out int maxEp,
                        m.SI, Rexps, out FilenameProcessorRegEx rex);
                    var matchSeasonEpisode = m.SI.DVDOrder
                        ? seasF == m.AiredSeasonNumber && epF == m.AiredEpNum
                        : seasF == m.DVDSeasonNumber && epF == m.DVDEpNum;
                    if (findFile && matchSeasonEpisode)
                    {
                        // match!
                        // get extension from nameInTorrent
                        var p = nameInTorrent.LastIndexOf(".");
                        var ext = p == -1 ? "" : nameInTorrent.Substring(p);
                        AlterResume(torrentFile, torrentFileNum, name + ext);
                        if (SetPrios)
                            SetResumePrio(torrentFile, torrentFileNum, BTPrio.Normal);
                        return new FileInfo(name + ext);
                    }
                }
            }

            return null;
        }

        public void WriteResumeDat()
        {
            FixFileguard();
            // write out new resume.dat file
            var to = ResumeDatPath + ".before_tvrename";
            if (File.Exists(to))
                File.Delete(to);
            File.Move(ResumeDatPath, to);
            Stream s = File.Create(ResumeDatPath);
            ResumeDat.Write(s);
            s.Close();
        }

        public override bool NewTorrentEntry(string torrentFile, int numberInTorrent)
        {
            NewLocation = "";
            PrioWasSet = false;
            Type = "?";
            return true;
        }

        public override bool FoundFileOnDiskForFileInTorrent(string torrentFile, FileInfo onDisk, int numberInTorrent,
            string nameInTorrent)
        {
            NewLocation = onDisk.FullName;
            Type = "Hash";

            AlterResume(torrentFile, numberInTorrent, onDisk.FullName); // make resume.dat point to the file we found

            if (SetPrios)
                SetResumePrio(torrentFile, numberInTorrent, BTPrio.Normal);

            return true;
        }

        public override bool DidNotFindFileOnDiskForFileInTorrent(string torrentFile, int numberInTorrent,
            string nameInTorrent)
        {
            Type = "Not Found";

            if (SetPrios)
                SetResumePrio(torrentFile, numberInTorrent, BTPrio.Skip);
            return true;
        }

        public override bool FinishedTorrentEntry(string torrentFile, int numberInTorrent, string filename)
        {
            if (DoMatchMissing)
            {
                var s = MatchMissing(torrentFile, numberInTorrent, filename);
                if (s != null)
                {
                    PrioWasSet = true;
                    NewLocation = s.FullName;
                    Type = "Missing";
                }
            }

            if (SetPrios && !PrioWasSet)
            {
                SetResumePrio(torrentFile, numberInTorrent, BTPrio.Skip);
                Type = "Not Missing";
            }

            var prioChanged = SetPrios && PrioWasSet;
            if (prioChanged || !string.IsNullOrEmpty(NewLocation))
                AddResult(Type, torrentFile, (numberInTorrent + 1).ToString(),
                    prioChanged ? GetResumePrio(torrentFile, numberInTorrent) : "", NewLocation);
            return true;
        }

        public bool LoadResumeDat()
        {
            var bel = new BtEncodeLoader();
            ResumeDat = bel.Load(ResumeDatPath);
            return ResumeDat != null;
        }

        //public bool DoWork(List<string> Torrents, string searchFolder, ListView results, bool hashSearch, bool matchMissing, bool setPrios, bool testMode,
        //    bool searchSubFolders, ItemList missingList, List<FilenameProcessorRegEx> rexps, CommandLineArgs args)
        public bool DoWork(List<string> Torrents, string searchFolder, bool hashSearch, bool matchMissing,
            bool setPrios, bool testMode,
            bool searchSubFolders, ItemList missingList, List<FilenameProcessorRegEx> rexps, CommandLineArgs args)
        {
            Rexps = rexps;

            if (!matchMissing && !hashSearch)
                return true; // nothing to do

            if (hashSearch && string.IsNullOrEmpty(searchFolder))
                return false;

            if (matchMissing && (missingList == null || rexps == null))
                return false;

            MissingList = missingList;
            DoMatchMissing = matchMissing;
            DoHashChecking = hashSearch;
            SetPrios = setPrios;
            //Results = results;

            Prog(0);

            if (!LoadResumeDat())
                return false;

            var r = true;

            Prog(0);

            if (hashSearch)
                BuildFileCache(searchFolder, searchSubFolders);

            foreach (var tf in Torrents)
            {
                r = ProcessTorrentFile(tf); //TODO: Put this back, null, args);
                if (!r) // stop on the first failure
                    break;
            }

            if (Altered && !testMode)
                WriteResumeDat();

            Prog(0);

            return r;
        }

        public static string RemoveUT(string s)
        {
            // if it is a .!ut file, we can remove the extension
            if (s.EndsWith(".!ut"))
                return s.Remove(s.Length - 4);
            return s;
        }

        public void AddResult(string type, string torrent, string num, string prio, string location)
        {
            // TODO: Put this back

            //if (Results == null)
            //    return;

            //int p = torrent.LastIndexOf(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
            //if (p != -1)
            //    torrent = torrent.Substring(p + 1);
            //ListViewItem lvi = new ListViewItem(type);
            //lvi.SubItems.Add(torrent);
            //lvi.SubItems.Add(num);
            //lvi.SubItems.Add(prio);
            //lvi.SubItems.Add(RemoveUT(location));

            //Results.Items.Add(lvi);
            //lvi.EnsureVisible();
            //Results.Update();
        }

        private static class BTPrio
        {
            public const int Normal = 0x08, Skip = 0x80;
        }
    }
}
