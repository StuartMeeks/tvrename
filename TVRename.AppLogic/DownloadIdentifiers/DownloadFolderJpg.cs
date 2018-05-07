using System.Collections.Generic;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.Settings;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;

namespace TVRename.AppLogic.DownloadIdentifiers
{
    public class DownloadFolderJpg : DownloadIdentifier
    {
        private List<string> _doneFolderJpg;
        private const string DefaultFileName = "folder.jpg";

        public DownloadFolderJpg() 
        {
            Reset();
        }

        public override DownloadType GetDownloadType() => DownloadType.DownloadImage;

        public override ItemList ProcessShow(ProcessedSeries si, bool forceRefresh)
        {
            if (ApplicationSettings.Instance.FolderJpg)
            {
                ItemList actionList = new ItemList();
                FileInfo fi = FileHelper.FileInFolder(si.AutoAdd_FolderBase, DefaultFileName);
                bool fileDoesntExist = !_doneFolderJpg.Contains(fi.FullName) && !fi.Exists;

                if (forceRefresh || fileDoesntExist)
                {
                    string downloadPath = ApplicationSettings.Instance.SeasonSpecificFolderJPG()
                        ? si.TheSeries().GetSeriesPosterPath()
                        : si.TheSeries().GetImage(ApplicationSettings.Instance.ItemForFolderJpg());

                    if (!string.IsNullOrEmpty(downloadPath))
                    {
                        actionList.Add(new DownloadImageActionItem(si, null, fi, downloadPath));
                    }

                    _doneFolderJpg.Add(fi.FullName);
                }
                return actionList;

            }
            return null;
        }

        public override ItemList ProcessSeason(ProcessedSeries si, string folder, int snum, bool forceRefresh)
        {
            if (ApplicationSettings.Instance.FolderJpg)
            {
                // season folders JPGs

                ItemList actionList = new ItemList();
                FileInfo fi = FileHelper.FileInFolder(folder, DefaultFileName);
                if (!_doneFolderJpg.Contains(fi.FullName) && (!fi.Exists|| forceRefresh))
                // some folders may come up multiple times
                {
                    string bannerPath = ApplicationSettings.Instance.SeasonSpecificFolderJPG()
                        ? si.TheSeries().GetSeasonBannerPath(snum)
                        : si.TheSeries().GetImage(ApplicationSettings.Instance.ItemForFolderJpg());

                    if (!string.IsNullOrEmpty(bannerPath))
                    {
                        actionList.Add(new DownloadImageActionItem(si, null, fi, bannerPath, ApplicationSettings.Instance.ShrinkLargeMede8erImages));

                    }

                    _doneFolderJpg.Add(fi.FullName);
                }
                return actionList;
            }

            
            return base.ProcessSeason(si,folder,snum,forceRefresh);
        }
        public sealed override void Reset()
        {
            _doneFolderJpg  = new List<string>();
        }
    }

}
