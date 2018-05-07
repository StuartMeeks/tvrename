using Alphaleonis.Win32.Filesystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TVRename.AppLogic.Delegates;
using TVRename.AppLogic.DownloadIdentifiers;
using TVRename.AppLogic.FileSystemCache;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.Settings;

namespace TVRename.AppLogic.Finders
{
    internal class FileFinder : FinderBase
    {
        public FileFinder(TvRenameManager tvRenameManager)
            : base(tvRenameManager)
        {

        }

        // TODO: Put this back
        // private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public override bool Active() => ApplicationSettings.Instance.SearchLocally;

        public override FinderDisplayType DisplayType() => FinderDisplayType.Local;

        public override void Check(ProgressUpdatedDelegate progressDelegate, int startPercent, int totalPercent)
        {
            progressDelegate.Invoke(startPercent);

            ItemList newList = new ItemList();
            ItemList toRemove = new ItemList();

            int totalFiles = 0, searchedFiles = 0;
            foreach (string searchFolder in TvRenameManager.SearchFolders)
            {
                totalFiles += DirectoryCache.CountFiles(searchFolder, true);
            }

            DirectoryCache directoryCache = new DirectoryCache();
            foreach (string searchFolder in TvRenameManager.SearchFolders)
            {
                if (Cancel)
                {
                    return;
                }

                directoryCache.AddFolder(progressDelegate, searchedFiles, totalFiles, searchFolder, true);
            }

            int currentItem = 0;
            int totalN = ActionList.Count;
            foreach (ItemBase action1 in ActionList)
            {
                if (Cancel)
                {
                    return;
                }

                progressDelegate.Invoke(startPercent + (totalPercent - startPercent) * (++currentItem) / (totalN + 1));

                if (!(action1 is MissingItem me))
                {
                    continue;
                }

                int numberMatched = 0;
                ItemList thisRound = new ItemList();
                DirectoryCacheItem matchedFile = null;

                foreach (DirectoryCacheItem dce in directoryCache)
                {
                    if (ReviewFile(me, thisRound, dce))
                    {
                        numberMatched++;
                        matchedFile = dce;
                    }
                }

                if (numberMatched == 1)
                {
                    if (!OtherActionsMatch(matchedFile, me, ActionList))
                    {
                        toRemove.Add(action1);
                        newList.AddRange(thisRound);
                    }
                    else
                    {
                        // TODO: Put this back
                        //logger.Warn($"Ignoring potential match for {action1.Episode.SI.ShowName} S{action1.Episode.AppropriateSeasonNumber} E{action1.Episode.AppropriateEpNum}: with file {matchedFile.TheFile.FullName} as there are multiple actions for that file");
                    }
                }
                else if (numberMatched > 1)
                {
                    // TODO: Put this back
                    // logger.Warn($"Ignoring potential match for {action1.Episode.SI.ShowName} S{action1.Episode.AppropriateSeasonNumber} E{action1.Episode.AppropriateEpNum}: with file {matchedFile.TheFile.FullName} as there are multiple files for that action");
                }

            }

            if (ApplicationSettings.Instance.KeepTogether)
            {
                KeepTogether(newList);
            }

            progressDelegate.Invoke(totalPercent);

            if (!ApplicationSettings.Instance.LeaveOriginals)
            {
                // go through and change last of each operation on a given source file to a 'Move'
                // ideally do that move within same filesystem

                // sort based on source file, and destination drive, putting last if destdrive == sourcedrive
                newList.Sort(new ActionItemSorter());

                // sort puts all the CopyMoveRenames together				

                // then set the last of each source file to be a move
                for (int i = 0; i < newList.Count; i++)
                {
                    CopyMoveRenameActionItem cmr1 = newList[i] as CopyMoveRenameActionItem;
                    bool ok1 = cmr1 != null;

                    if (!ok1)
                        continue;

                    bool last = i == (newList.Count - 1);
                    CopyMoveRenameActionItem cmr2 = !last ? newList[i + 1] as CopyMoveRenameActionItem : null;
                    bool ok2 = cmr2 != null;

                    if (ok2)
                    {
                        CopyMoveRenameActionItem a1 = cmr1;
                        CopyMoveRenameActionItem a2 = cmr2;
                        if (!FileHelper.IsSameFile(a1.SourceFile, a2.SourceFile))
                            a1.Operation = FileOperation.Move;
                    }
                    else
                    {
                        // last item, or last copymoverename item in the list
                        CopyMoveRenameActionItem a1 = cmr1;
                        a1.Operation = FileOperation.Move;
                    }
                }
            }

            foreach (ItemBase i in toRemove)
            {
                ActionList.Remove(i);
            }

            foreach (ItemBase i in newList)
            {
                ActionList.Add(i);
            }
        }

        private bool OtherActionsMatch(DirectoryCacheItem matchedFile, MissingItem me, ItemList actionList)
        //This is used to check whether the selected file may match any other files we are looking for
        {
            foreach (ItemBase testAction in actionList)
            {
                if (!(testAction is MissingItem testMissingAction)) continue;
                if (testMissingAction.Equals(me))
                {
                    continue;
                }

                if (ReviewFile(testMissingAction, new ItemList(), matchedFile)) return true;

            }

            return false;
        }

        private void KeepTogether(ItemList actionlist)
        {
            // for each of the items in rcl, do the same copy/move if for other items with the same
            // base name, but different extensions

            ItemList extras = new ItemList();

            foreach (ItemBase action1 in actionlist)
            {
                if (!(action1 is CopyMoveRenameActionItem))
                    continue;

                CopyMoveRenameActionItem action = (CopyMoveRenameActionItem)(action1);

                try
                {
                    DirectoryInfo sfdi = action.SourceFile.Directory;
                    string basename = action.SourceFile.Name;
                    int l = basename.Length;
                    basename = basename.Substring(0, l - action.SourceFile.Extension.Length);

                    string toname = action.TargetFile.Name;
                    int l2 = toname.Length;
                    toname = toname.Substring(0, l2 - action.TargetFile.Extension.Length);

                    FileInfo[] flist = sfdi.GetFiles(basename + ".*");
                    foreach (FileInfo fi in flist)
                    {
                        //check to see whether the file is one of the types we do/don't want to include
                        if (!ApplicationSettings.Instance.KeepTogetherFilesWithType(fi.Extension)) continue;

                        // do case insensitive replace
                        string n = fi.Name;
                        int p = n.IndexOf(basename, StringComparison.OrdinalIgnoreCase);
                        string newName = n.Substring(0, p) + toname + n.Substring(p + basename.Length);
                        if (ApplicationSettings.Instance.RenameTxtToSub && newName.EndsWith(".txt"))
                        {
                            newName = newName.Substring(0, newName.Length - 4) + ".sub";
                        }

                        CopyMoveRenameActionItem newitem = new CopyMoveRenameActionItem(fi,
                            FileHelper.FileInFolder(action.TargetFile.Directory, newName), action.Operation,
                            action.ItemEpisode, null, null);

                        // check this item isn't already in our to-do list
                        bool doNotAdd = false;
                        foreach (ItemBase ai2 in actionlist)
                        {
                            if (!(ai2 is CopyMoveRenameActionItem))
                            {
                                continue;
                            }

                            if (((CopyMoveRenameActionItem)ai2).IsSameSource(newitem))
                            {
                                doNotAdd = true;
                                break;
                            }
                        }

                        if (!doNotAdd)
                        {
                            if (!newitem.Equals(action)) // don't re-add ourself
                            {
                                extras.Add(newitem);
                            }
                        }
                    }
                }
                catch (System.IO.PathTooLongException e)
                {
                    string t = "Path or filename too long. " + action.SourceFile.FullName + ", " + e.Message;

                    // TODO: Put this back
                    // logger.Warn(e, "Path or filename too long. " + action.SourceFile.FullName);

                    if ((!TvRenameManager.Args.Unattended) && (!TvRenameManager.Args.Hide))
                    {
                        // TODO: Figure out a delegate for raising events to show a message box in the UI
                        // MessageBox.Show(t, "Path or filename too long", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
            }

            foreach (ItemBase action in extras)
            {
                // check we don't already have this in our list and, if we don't add it!
                bool have = false;
                foreach (ItemBase action2 in actionlist)
                {
                    if (action2.Equals(action))
                    {
                        have = true;
                        break;
                    }

                    if ((action is ActionItemBase a1) && (action2 is ActionItemBase))
                    {
                        ActionItemBase a2 = (ActionItemBase)action2;
                        if (a2.ActionProduces == a1.ActionProduces)
                        {
                            have = true;
                            break;
                        }
                    }
                }

                if (!have)
                {
                    // put before other actions, so tidyup is run last
                    actionlist.Insert(0, action);
                }
            }
        }
        // consider each of the files, see if it is suitable for series "ser" and episode "epi"
        // if so, add a rcitem for copy to "fi"


        private bool ReviewFile(MissingItem me, ItemList addTo, DirectoryCacheItem dce)
        {
            int season = me.ItemEpisode.AppropriateSeasonNumber;
            int epnum = me.ItemEpisode.AppropriateEpNum;

            if (Cancel)
            {
                return true;
            }

            bool matched = false;

            try
            {
                if (FileHelper.IgnoreFile(dce.File))
                {
                    return false;
                }

                //do any of the possible names for the series match the filename?
                matched = me.ItemEpisode.SI.getSimplifiedPossibleShowNames()
                    .Any(name => FileHelper.SimplifyAndCheckFilename(dce.SimplifiedFullName, name));

                if (matched)
                {
                    if (
                        (TvRenameManager.FindSeasEp(dce.File, out int seasF, out int epF, out int maxEp, me.ItemEpisode.SI)
                         && seasF == season
                         && epF == epnum)
                        ||
                        (TvRenameManager.MatchesSequentialNumber(dce.File.Name, ref seasF, ref epF, me.ItemEpisode)
                         && me.ItemEpisode.SI.UseSequentialMatch
                         && seasF == season
                         && epF == epnum)
                    )
                    {
                        if (maxEp != -1 && ApplicationSettings.Instance.AutoMergeEpisodes)
                        {
                            SeasonRule sr = new SeasonRule()
                            {
                                Action = RuleAction.Merge,
                                First = epF,
                                Second = maxEp
                            };

                            if (!me.ItemEpisode.SI.SeasonRules.ContainsKey(seasF))
                            {
                                me.ItemEpisode.SI.SeasonRules[seasF] = new List<SeasonRule>();
                            }

                            me.ItemEpisode.SI.SeasonRules[seasF].Add(sr);

                            // TODO: Put this back
                            //logger.Info($"Looking at {me.Episode.SI.ShowName} and have identified that episode {epF} and {maxEp} of season {seasF} have been merged into one file {dce.TheFile.FullName}");
                            //logger.Info($"Added new rule automatically for {sr}");

                            //Regenerate the episodes with the new rule added
                            TvRenameManager.GenerateEpisodeDict(me.ItemEpisode.SI);

                            //Get the newly created processed episode we are after
                            List<ProcessedEpisode> newSeason = TvRenameManager.GetShowItem(me.ItemEpisode.SI.TVDBCode).SeasonEpisodes[seasF];
                            ProcessedEpisode newPE = me.ItemEpisode;

                            foreach (ProcessedEpisode pe in newSeason)
                            {
                                if (pe.AppropriateEpNum == epF && pe.EpNum2 == maxEp) newPE = pe;
                            }

                            me = new MissingItem(newPE, me.ItemTargetFolder, ApplicationSettings.Instance.FilenameFriendly(ApplicationSettings.Instance.NamingStyle.NameForExt(newPE)));
                        }

                        FileInfo fi = new FileInfo(me.TheFileNoExt + dce.File.Extension);

                        if (ApplicationSettings.Instance.PreventMove)
                        {
                            //We do not want to move the file, just rename it
                            fi = new FileInfo(dce.File.DirectoryName + System.IO.Path.DirectorySeparatorChar + me.Filename + dce.File.Extension);
                        }

                        // don't remove the base search folders
                        bool doTidyup = true;
                        foreach (string searchFolder in this.TvRenameManager.SearchFolders)
                        {
                            if (searchFolder.IsSameDirectoryLocation(fi.Directory.FullName))


                            {
                                doTidyup = false;
                                break;
                            }
                        }

                        if (dce.File.FullName != fi.FullName)
                        {
                            addTo.Add(new CopyMoveRenameActionItem(dce.File, fi, FileOperation.Copy, me.ItemEpisode, doTidyup ? ApplicationSettings.Instance.Tidyup : null, me));
                        }

                        DownloadIdentifiersController di = new DownloadIdentifiersController();

                        // if we're copying/moving a file across, we might also want to make a thumbnail or NFO for it
                        addTo.Add(di.ProcessEpisode(me.ItemEpisode, fi));

                        return true;
                    }
                }
            }
            catch (System.IO.PathTooLongException e)
            {
                string t = "Path too long. " + dce.File.FullName + ", " + e.Message;

                // TODO: Put this back
                // logger.Warn(e, "Path too long. " + dce.File.FullName);

                t += ".  More information is available in the log file";
                if ((!this.TvRenameManager.Args.Unattended) && (!this.TvRenameManager.Args.Hide))
                {
                    // TODO: Figure out message boxes
                    // MessageBox.Show(t, "Path too long", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }

                t = "DirectoryName " + dce.File.DirectoryName + ", File name: " + dce.File.Name;
                t += matched ? ", matched.  " : ", no match.  ";
                if (matched)
                {
                    t += "Show: " + me.ItemEpisode.TheSeries.Name + ", Season " + season + ", Ep " + epnum + ".  ";
                    t += "To: " + me.TheFileNoExt;
                }

                // TODO: Put this back
                //logger.Warn(t);
            }

            return false;
        }
    }
}

