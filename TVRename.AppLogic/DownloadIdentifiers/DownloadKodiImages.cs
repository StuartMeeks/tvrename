using System.Collections.Generic;
using System.Globalization;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.Settings;
using TVRename.AppLogic.TheTvDb;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;

namespace TVRename.AppLogic.DownloadIdentifiers
{
    public class DownloadKodiImages : DownloadIdentifier
    {
        private List<string> _donePosterJpg;
        private List<string> _doneBannerJpg;
        private List<string> _doneFanartJpg;
        private List<string> _doneTbn;

        public DownloadKodiImages() 
        {
            Reset();
        }

        public override DownloadType GetDownloadType() => DownloadType.DownloadImage;

        public override void NotifyComplete(FileInfo file)
        {
            if (file.Name.EndsWith(".tbn", true, new CultureInfo("en")))
            {
                _doneTbn.Add(file.FullName);
            } 
            base.NotifyComplete(file);
        }

        public override ItemList ProcessShow(ProcessedSeries si, bool forceRefresh)
        {
            //If we have KODI New style images being downloaded then we want to check that 3 files exist
            //for the series:
            //http://wiki.xbmc.org/index.php?title=XBMC_v12_(Frodo)_FAQ#Local_images
            //poster
            //banner
            //fanart

            if (ApplicationSettings.Instance.KODIImages)
            {
                ItemList actionList = new ItemList();
                // base folder:
                if (!string.IsNullOrEmpty(si.AutoAdd_FolderBase) && (si.AllFolderLocations(false).Count > 0))
                {
                    FileInfo posterJpg = FileHelper.FileInFolder(si.AutoAdd_FolderBase, "poster.jpg");
                    FileInfo bannerJpg = FileHelper.FileInFolder(si.AutoAdd_FolderBase, "banner.jpg");
                    FileInfo fanartJpg = FileHelper.FileInFolder(si.AutoAdd_FolderBase, "fanart.jpg");

                    if ((forceRefresh || (!posterJpg.Exists)) && (!_donePosterJpg.Contains(si.AutoAdd_FolderBase)))
                    {
                        string path = si.TheSeries().GetSeriesPosterPath();
                        if (!string.IsNullOrEmpty(path))
                        {
                            actionList.Add(new DownloadImageActionItem(si, null, posterJpg, path));
                            _donePosterJpg.Add(si.AutoAdd_FolderBase);
                        }
                    }

                    if ((forceRefresh || (!bannerJpg.Exists)) && (!_doneBannerJpg.Contains(si.AutoAdd_FolderBase)))
                    {
                        string path = si.TheSeries().GetSeriesWideBannerPath();
                        if (!string.IsNullOrEmpty(path))
                        {
                            actionList.Add(new DownloadImageActionItem(si, null, bannerJpg, path));
                            _doneBannerJpg.Add(si.AutoAdd_FolderBase);
                        }
                    }

                    if ((forceRefresh || (!fanartJpg.Exists)) && (!_doneFanartJpg.Contains(si.AutoAdd_FolderBase)))
                    {
                        string path = si.TheSeries().GetSeriesFanartPath();
                        if (!string.IsNullOrEmpty(path))
                        {
                            actionList.Add(new DownloadImageActionItem(si, null, fanartJpg, path));
                            _doneFanartJpg.Add(si.AutoAdd_FolderBase);
                        }
                    }
                }
                return actionList;
            }

            return base.ProcessShow(si, forceRefresh);
        }

        public override ItemList ProcessSeason(ProcessedSeries si, string folder, int snum, bool forceRefresh)
        {
            if (ApplicationSettings.Instance.KODIImages)
            {
                ItemList actionList = new ItemList();
                if (ApplicationSettings.Instance.DownloadFrodoImages())
                {
                    //If we have KODI New style images being downloaded then we want to check that 3 files exist
                    //for the series:
                    //http://wiki.xbmc.org/index.php?title=XBMC_v12_(Frodo)_FAQ#Local_images
                    //poster
                    //banner
                    //fanart - we do not have the option in TVDB to get season specific fanart, so we'll leave that
                    string filenamePrefix = "";

                    if (!si.AutoAdd_FolderPerSeason)
                    {   // We have multiple seasons in the same folder
                        // We need to do slightly more work to come up with the filenamePrefix
                        filenamePrefix = "season";

                        if (snum == 0) filenamePrefix += "-specials";
                        else if (snum < 10) filenamePrefix += "0" + snum;
                        else filenamePrefix += snum;

                        filenamePrefix += "-";
                    }

                    FileInfo posterJpg = FileHelper.FileInFolder(folder, filenamePrefix + "poster.jpg");
                    if (forceRefresh || !posterJpg.Exists)
                    {
                        string path = si.TheSeries().GetSeasonBannerPath(snum);
                        if (!string.IsNullOrEmpty(path))
                        {
                            actionList.Add(new DownloadImageActionItem(si, null, posterJpg, path));
                        }
                    }

                    FileInfo bannerJPG = FileHelper.FileInFolder(folder, filenamePrefix + "banner.jpg");
                    if (forceRefresh || !bannerJPG.Exists)
                    {
                        string path = si.TheSeries().GetSeasonWideBannerPath(snum);
                        if (!string.IsNullOrEmpty(path))
                        {
                            actionList.Add(new DownloadImageActionItem(si, null, bannerJPG, path));
                        }
                    }
                }
                if (ApplicationSettings.Instance.DownloadEdenImages())
                {
                    string filenamePrefix = "season";

                    if (snum == 0)
                    {
                        filenamePrefix += "-specials";
                    }
                    else if (snum < 10)
                    {
                        filenamePrefix += "0" + snum;
                    }
                    else
                    {
                        filenamePrefix += snum;
                    }

                    FileInfo posterTbn = FileHelper.FileInFolder(si.AutoAdd_FolderBase, filenamePrefix + ".tbn");
                    if (forceRefresh || !posterTbn.Exists)
                    {
                        string path = si.TheSeries().GetSeasonBannerPath(snum);
                        if (!string.IsNullOrEmpty(path))
                        {
                            actionList.Add(new DownloadImageActionItem(si, null, posterTbn, path));
                        }
                    }
                }
                return actionList;
            }

            return base.ProcessSeason(si, folder, snum, forceRefresh);
        }

        public override ItemList ProcessEpisode(ProcessedEpisode dbep, FileInfo filo, bool forceRefresh)
        {
            if (ApplicationSettings.Instance.EpTBNs || ApplicationSettings.Instance.KODIImages)
            {
                ItemList actionList = new ItemList();

                if (dbep.type == ProcessedEpisode.ProcessedEpisodeType.merged)
                {
                    //We have a merged episode, so we'll also download the images for the episodes had they been separate.
                    foreach (TheTvDbEpisode sourceEp in dbep.sourceEpisodes)
                    {
                        string foldername = filo.DirectoryName;
                        string filename = ApplicationSettings.Instance.FilenameFriendly(
                            ApplicationSettings.Instance.NamingStyle.GetTargetEpisodeName(sourceEp, dbep.SI.ShowName,
                                dbep.SI.GetTimeZone(), dbep.SI.DVDOrder));

                        DownloadImageActionItem b = DoEpisode(dbep.SI,sourceEp,new FileInfo(foldername+"/"+filename), ".jpg", forceRefresh);

                        if (b != null)
                        {
                            actionList.Add(b);
                        }
                    }
                }
                else
                {
                    DownloadImageActionItem a = DoEpisode(dbep.SI, dbep, filo, ".tbn", forceRefresh);
                    if (a != null)
                    {
                        actionList.Add(a);
                    }
                }

                return actionList;
            }
            return base.ProcessEpisode(dbep, filo, forceRefresh);
        }

        private DownloadImageActionItem DoEpisode(ProcessedSeries si, TheTvDbEpisode ep, FileInfo filo,string extension, bool forceRefresh)
        {
            string ban = ep.GetFilename();
            if (!string.IsNullOrEmpty(ban))
            {
                string basefn = filo.RemoveExtension();
                FileInfo imgtbn = FileHelper.FileInFolder(filo.Directory, basefn + extension);

                if (imgtbn.Exists && !forceRefresh)
                {
                    return null;
                }

                if (!_doneTbn.Contains(imgtbn.FullName))
                {
                    _doneTbn.Add(imgtbn.FullName);

                    return new DownloadImageActionItem(si,
                        ep is ProcessedEpisode episode ? episode : new ProcessedEpisode(ep, si), imgtbn,
                        ban);
                }
            }

            return null;

        }

        public sealed override void Reset()
        {
            _doneBannerJpg = new List<string>();
            _donePosterJpg = new List<string>();
            _doneFanartJpg = new List<string>();
            _doneTbn = new List<string>();
        }

    }
}
