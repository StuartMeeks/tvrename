using System;
using System.Collections.Generic;
using Stream = System.IO.Stream;
using MemoryStream = System.IO.MemoryStream;
using PathTooLongException = System.IO.PathTooLongException;

using TVRename.AppLogic.Delegates;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.Settings;

using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace TVRename.AppLogic.BitTorrent
{
    public class BtResume : BtCore
    {
        private static class BTPrio { public const int Normal = 0x08, Skip = 0x80; }

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

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public BtResume(ProgressUpdatedDelegate setprog, string resumeDatFile)
            : base(setprog)
        {
            this.ResumeDatPath = resumeDatFile;
        }

        public BtDictionary GetTorrentDict(string torrentFile)
        {
            // find dictionary for the specified torrent file

            BtItemBase it = this.ResumeDat.GetDict().GetItem(torrentFile, true);
            if ((it == null) || (it.Type != BtChunk.Dictionary))
                return null;
            BtDictionary dict = (BtDictionary)(it);
            return dict;
        }

        public static int PercentBitsOn(BtString s)
        {
            int totalBits = 0;
            int bitsOn = 0;

            for (int i = 0; i < s.Data.Length; i++)
            {
                totalBits += 8;
                byte c = s.Data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((c & 0x01) != 0)
                        bitsOn++;
                    c >>= 1;
                }
            }

            return (100 * bitsOn + totalBits / 2) / totalBits;
        }

        public List<TorrentEntry> AllFilesBeingDownloaded()
        {
            System.Collections.Generic.List<TorrentEntry> r = new System.Collections.Generic.List<TorrentEntry>();

            BtEncodeLoader bel = new BtEncodeLoader();
            foreach (BtDictionaryItem it in this.ResumeDat.GetDict().Items)
            {
                if ((it.Type != BtChunk.DictionaryItem))
                    continue;

                BtDictionaryItem dictitem = (BtDictionaryItem)(it);

                if ((dictitem.Key == ".fileguard") || (dictitem.Data.Type != BtChunk.Dictionary))
                    continue;

                string torrentFile = dictitem.Key;
                BtDictionary d2 = (BtDictionary)(dictitem.Data);

                BtItemBase p = d2.GetItem("prio");
                if ((p == null) || (p.Type != BtChunk.String))
                    continue;

                BtString prioString = (BtString)(p);
                string directoryName = Alphaleonis.Win32.Filesystem.Path.GetDirectoryName(this.ResumeDatPath) + System.IO.Path.DirectorySeparatorChar;

                if (!Alphaleonis.Win32.Filesystem.File.Exists(torrentFile)) // if the torrent file doesn't exist
                    torrentFile = directoryName + torrentFile; // ..try prepending the resume.dat folder's path to it.

                if (!Alphaleonis.Win32.Filesystem.File.Exists(torrentFile))
                    continue; // can't find it.  give up!

                BtFile tor = bel.Load(torrentFile);
                if (tor == null)
                    continue;

                List<string> a = tor.AllFilesInTorrent();
                if (a != null)
                {
                    int c = 0;

                    p = d2.GetItem("path");
                    if ((p == null) || (p.Type != BtChunk.String))
                        continue;
                    string defaultFolder = ((BtString)p).AsString();

                    BtItemBase targets = d2.GetItem("targets");
                    bool hasTargets = ((targets != null) && (targets.Type == BtChunk.List));
                    BtList targetList = (BtList)(targets);

                    foreach (string s in a)
                    {
                        if ((c < prioString.Data.Length) && (prioString.Data[c] != BTPrio.Skip))
                        {
                            try
                            {
                                string saveTo = FileHelper.FileInFolder(defaultFolder, ApplicationSettings.Instance.FilenameFriendly(s)).Name;
                                if (hasTargets)
                                {
                                    // see if there is a target for this (the c'th) file
                                    for (int i = 0; i < targetList.Items.Count; i++)
                                    {
                                        BtList l = (BtList)(targetList.Items[i]);
                                        BtInteger n = (BtInteger)(l.Items[0]);
                                        BtString dest = (BtString)(l.Items[1]);
                                        if (n.Value == c)
                                        {
                                            saveTo = dest.AsString();
                                            break;
                                        }
                                    }
                                }
                                int percent = (a.Count == 1) ? PercentBitsOn((BtString)(d2.GetItem("have"))) : -1;
                                TorrentEntry te = new TorrentEntry(torrentFile, saveTo, percent);
                                r.Add(te);

                            }
                            catch (PathTooLongException ptle)
                            {
                                //this is not the file we are looking for
                                logger.Debug(ptle);
                            }
                        }
                        c++;
                    }
                }
            }

            return r;
        }

        public string GetResumePrio(string torrentFile, int fileNum)
        {
            BtDictionary dict = this.GetTorrentDict(torrentFile);
            if (dict == null)
                return "";
            BtItemBase p = dict.GetItem("prio");
            if ((p == null) || (p.Type != BtChunk.String))
                return "";
            BtString prioString = (BtString)(p);
            if ((fileNum < 0) || (fileNum > prioString.Data.Length))
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
            if (!this.SetPrios)
                return;

            if (fileNum == -1)
                fileNum = 0;
            BtDictionary dict = this.GetTorrentDict(torrentFile);
            if (dict == null)
                return;
            BtItemBase p = dict.GetItem("prio");
            if ((p == null) || (p.Type != BtChunk.String))
                return;
            BtString prioString = (BtString)(p);
            if ((fileNum < 0) || (fileNum > prioString.Data.Length))
                return;

            this.Altered = true;
            this.PrioWasSet = true;

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

            BtDictionary dict = this.GetTorrentDict(torrentFile);
            if (dict == null)
                return;

            this.Altered = true;

            if (fileNum == -1) // single file torrent
            {
                BtItemBase p = dict.GetItem("path");
                if (p == null)
                    dict.Items.Add(new BtDictionaryItem("path", new BtString(toHere)));
                else
                {
                    if (p.Type != BtChunk.String)
                        return;
                    ((BtString)p).SetString(toHere);
                }
            }
            else
            {
                // multiple file torrent, uses a list called "targets"
                BtItemBase p = dict.GetItem("targets");
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
                    theList = (BtList)(p);
                }
                if (theList == null)
                    return;

                // the list contains two element lists, of integer/string which are filenumber/path

                BtList thisFileList = null;
                // see if this file is already in the list
                for (int i = 0; i < theList.Items.Count; i++)
                {
                    if (theList.Items[i].Type != BtChunk.List)
                        return;

                    BtList l2 = (BtList)(theList.Items[i]);
                    if ((l2.Items.Count != 2) || (l2.Items[0].Type != BtChunk.Integer) || (l2.Items[1].Type != BtChunk.String))
                        return;
                    int n = (int)((BtInteger)(l2.Items[0])).Value;
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
                    thisFileList.Items[1] = new BtString(toHere);
            }
        }

        public void FixFileguard()
        {
            // finally, fix up ".fileguard"
            // this is the SHA1 of the entire file, without the .fileguard
            this.ResumeDat.GetDict().RemoveItem(".fileguard");
            MemoryStream ms = new MemoryStream();
            this.ResumeDat.Write(ms);
            System.Security.Cryptography.SHA1Managed sha1 = new System.Security.Cryptography.SHA1Managed();
            byte[] theHash = sha1.ComputeHash(ms.GetBuffer(), 0, (int)ms.Length);
            ms.Close();
            string newfg = BtString.CharsToHex(theHash, 0, 20);
            this.ResumeDat.GetDict().Items.Add(new BtDictionaryItem(".fileguard", new BtString(newfg)));
        }

        public FileInfo MatchMissing(string torrentFile, int torrentFileNum, string nameInTorrent)
        {
            // returns true if we found a match (if actSetPrio is on, true also means we have set a priority for this file)
            string simplifiedfname = FileHelper.SimplifyName(nameInTorrent);

            foreach (ItemBase Action1 in this.MissingList)
            {
                if ((!(Action1 is MissingItem)) && (!(Action1 is UTorrentingItem)) && (!(Action1 is SabNzbDItem)))
                    continue;

                ProcessedEpisode m = null;
                string name = null;

                if (Action1 is MissingItem)
                {
                    MissingItem Action = (MissingItem)(Action1);
                    m = Action.ItemEpisode;
                    name = Action.TheFileNoExt;
                }
                else if (Action1 is UTorrentingItem)
                {
                    UTorrentingItem Action = (UTorrentingItem)(Action1);
                    m = Action.ItemEpisode;
                    name = Action.DesiredLocationNoExt;
                }
                else if (Action1 is SabNzbDItem)
                {
                    SabNzbDItem Action = (SabNzbDItem)(Action1);
                    m = Action.ItemEpisode;
                    name = Action.DesiredLocationNoExt;
                }

                if ((m == null) || string.IsNullOrEmpty(name))
                    continue;

                // see if the show name matches...
                if (FileHelper.SimplifyAndCheckFilename(simplifiedfname, m.TheSeries.Name, false, true))
                {
                    // see if season and episode match
                    bool findFile = TVDoc.FindSeasEp("", simplifiedfname, out int seasF, out int epF, out int maxEp, m.SI, this.Rexps, out FilenameProcessorRegEx rex);
                    bool matchSeasonEpisode = m.SI.DVDOrder
                        ? (seasF == m.AiredSeasonNumber) && (epF == m.AiredEpNum)
                        : (seasF == m.DVDSeasonNumber) && (epF == m.DVDEpNum);
                    if (findFile && matchSeasonEpisode)
                    {
                        // match!
                        // get extension from nameInTorrent
                        int p = nameInTorrent.LastIndexOf(".");
                        string ext = (p == -1) ? "" : nameInTorrent.Substring(p);
                        this.AlterResume(torrentFile, torrentFileNum, name + ext);
                        if (this.SetPrios)
                            this.SetResumePrio(torrentFile, torrentFileNum, BTPrio.Normal);
                        return new FileInfo(name + ext);
                    }
                }
            }
            return null;
        }

        public void WriteResumeDat()
        {
            this.FixFileguard();
            // write out new resume.dat file
            string to = this.ResumeDatPath + ".before_tvrename";
            if (File.Exists(to))
                File.Delete(to);
            File.Move(this.ResumeDatPath, to);
            Stream s = File.Create(this.ResumeDatPath);
            this.ResumeDat.Write(s);
            s.Close();
        }

        public override bool NewTorrentEntry(string torrentFile, int numberInTorrent)
        {
            this.NewLocation = "";
            this.PrioWasSet = false;
            this.Type = "?";
            return true;
        }

        public override bool FoundFileOnDiskForFileInTorrent(string torrentFile, FileInfo onDisk, int numberInTorrent, string nameInTorrent)
        {
            this.NewLocation = onDisk.FullName;
            this.Type = "Hash";

            this.AlterResume(torrentFile, numberInTorrent, onDisk.FullName); // make resume.dat point to the file we found

            if (this.SetPrios)
                this.SetResumePrio(torrentFile, numberInTorrent, BTPrio.Normal);

            return true;
        }

        public override bool DidNotFindFileOnDiskForFileInTorrent(string torrentFile, int numberInTorrent, string nameInTorrent)
        {
            this.Type = "Not Found";

            if (this.SetPrios)
                this.SetResumePrio(torrentFile, numberInTorrent, BTPrio.Skip);
            return true;
        }

        public override bool FinishedTorrentEntry(string torrentFile, int numberInTorrent, string filename)
        {
            if (this.DoMatchMissing)
            {
                FileInfo s = this.MatchMissing(torrentFile, numberInTorrent, filename);
                if (s != null)
                {
                    this.PrioWasSet = true;
                    this.NewLocation = s.FullName;
                    this.Type = "Missing";
                }
            }

            if (this.SetPrios && !this.PrioWasSet)
            {
                this.SetResumePrio(torrentFile, numberInTorrent, BTPrio.Skip);
                this.Type = "Not Missing";
            }

            bool prioChanged = this.SetPrios && this.PrioWasSet;
            if (prioChanged || (!string.IsNullOrEmpty(this.NewLocation)))
                this.AddResult(this.Type, torrentFile, (numberInTorrent + 1).ToString(), prioChanged ? this.GetResumePrio(torrentFile, numberInTorrent) : "", this.NewLocation);
            return true;
        }

        public bool LoadResumeDat()
        {
            BtEncodeLoader bel = new BtEncodeLoader();
            this.ResumeDat = bel.Load(this.ResumeDatPath);
            return (this.ResumeDat != null);
        }

        //public bool DoWork(List<string> Torrents, string searchFolder, ListView results, bool hashSearch, bool matchMissing, bool setPrios, bool testMode,
        //    bool searchSubFolders, ItemList missingList, List<FilenameProcessorRegEx> rexps, CommandLineArgs args)
        public bool DoWork(List<string> Torrents, string searchFolder, bool hashSearch, bool matchMissing, bool setPrios, bool testMode,
            bool searchSubFolders, ItemList missingList, List<FilenameProcessorRegEx> rexps, CommandLineArgs args)
        {
            this.Rexps = rexps;

            if (!matchMissing && !hashSearch)
                return true; // nothing to do

            if (hashSearch && string.IsNullOrEmpty(searchFolder))
                return false;

            if (matchMissing && ((missingList == null) || (rexps == null)))
                return false;

            this.MissingList = missingList;
            this.DoMatchMissing = matchMissing;
            this.DoHashChecking = hashSearch;
            this.SetPrios = setPrios;
            //this.Results = results;

            this.Prog(0);

            if (!this.LoadResumeDat())
                return false;

            bool r = true;

            this.Prog(0);

            if (hashSearch)
                this.BuildFileCache(searchFolder, searchSubFolders);

            foreach (string tf in Torrents)
            {
                r = this.ProcessTorrentFile(tf); //TODO: Put this back, null, args);
                if (!r) // stop on the first failure
                    break;
            }

            if (this.Altered && !testMode)
                this.WriteResumeDat();

            this.Prog(0);

            return r;
        }

        public static string RemoveUT(string s)
        {
            // if it is a .!ut file, we can remove the extension
            if (s.EndsWith(".!ut"))
                return s.Remove(s.Length - 4);
            else
                return s;
        }

        public void AddResult(string type, string torrent, string num, string prio, string location)
        {
            // TODO: Put this back

            //if (this.Results == null)
            //    return;

            //int p = torrent.LastIndexOf(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
            //if (p != -1)
            //    torrent = torrent.Substring(p + 1);
            //ListViewItem lvi = new ListViewItem(type);
            //lvi.SubItems.Add(torrent);
            //lvi.SubItems.Add(num);
            //lvi.SubItems.Add(prio);
            //lvi.SubItems.Add(RemoveUT(location));

            //this.Results.Items.Add(lvi);
            //lvi.EnsureVisible();
            //this.Results.Update();
        }
    }
}
