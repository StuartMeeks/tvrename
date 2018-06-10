using System.Collections.Generic;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.ScanItems.Actions;
using TVRename.AppLogic.Settings;

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

        public override ItemList ProcessSeries(ProcessedSeries si, bool forceRefresh = false)
        {
            if (!ApplicationSettings.Instance.FolderJpg)
            {
                return null;
            }

            var actionList = new ItemList();
            var fi = FileHelper.FileInFolder(si.AutoAdd_FolderBase, DefaultFileName);
            var fileDoesntExist = !_doneFolderJpg.Contains(fi.FullName) && !fi.Exists;
            if (!forceRefresh && !fileDoesntExist)
            {
                return actionList;
            }

            var downloadPath = ApplicationSettings.Instance.SeasonSpecificFolderJPG()
                ? si.TheSeries().GetSeriesPosterPath()
                : si.TheSeries().GetImage(ApplicationSettings.Instance.ItemForFolderJpg());
            if (!string.IsNullOrEmpty(downloadPath))
            {
                actionList.Add(new DownloadImageAction(si, null, fi, downloadPath));
            }
            _doneFolderJpg.Add(fi.FullName);
            return actionList;
        }

        public override ItemList ProcessSeason(ProcessedSeries si, string folder, int snum, bool forceRefresh = false)
        {
            if (!ApplicationSettings.Instance.FolderJpg)
            {
                return base.ProcessSeason(si, folder, snum, forceRefresh);
            }

            // season folders JPGs
            var actionList = new ItemList();
            var fi = FileHelper.FileInFolder(folder, DefaultFileName);
            if (_doneFolderJpg.Contains(fi.FullName) || fi.Exists && !forceRefresh)
            {
                return actionList;
            }

            var bannerPath = ApplicationSettings.Instance.SeasonSpecificFolderJPG()
                ? si.TheSeries().GetSeasonBannerPath(snum)
                : si.TheSeries().GetImage(ApplicationSettings.Instance.ItemForFolderJpg());
            if (!string.IsNullOrEmpty(bannerPath))
            {
                actionList.Add(new DownloadImageAction(si, null, fi, bannerPath, ApplicationSettings.Instance.ShrinkLargeMede8erImages));

            }
            _doneFolderJpg.Add(fi.FullName);
            return actionList;


        }
        public sealed override void Reset()
        {
            _doneFolderJpg  = new List<string>();
        }
    }

}
