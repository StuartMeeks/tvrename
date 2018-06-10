using System.Collections.Generic;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.ScanItems.Actions;
using TVRename.AppLogic.Settings;

namespace TVRename.AppLogic.DownloadIdentifiers
{
    public class DownloadFanartJpg : DownloadIdentifier
    {
        private static List<string> _doneFanartJpg;
        private const string DefaultFileName = "fanart.jpg";

        public DownloadFanartJpg() 
        {
            Reset();
        }

        public override DownloadType GetDownloadType() => DownloadType.DownloadImage;

        public override ItemList ProcessSeries(ProcessedSeries si, bool forceRefresh = false)
        {
            //We only want to do something if the fanart option is enabled. If the KODI option is enabled then let it do the work.
            if ((!ApplicationSettings.Instance.FanArtJpg) || ApplicationSettings.Instance.KODIImages)
            {
                return base.ProcessSeries(si, forceRefresh);
            }

            var actionList = new ItemList();
            var fi = FileHelper.FileInFolder(si.AutoAdd_FolderBase, DefaultFileName);
            var doesntExist =  !fi.Exists;

            if (!forceRefresh && !doesntExist || _doneFanartJpg.Contains(fi.FullName))
            {
                return actionList;
            }

            var bannerPath = si.TheSeries().GetSeriesFanartPath();
            if (!string.IsNullOrEmpty(bannerPath))
            {
                actionList.Add(new DownloadImageAction(si, null, fi, bannerPath));
            }
            _doneFanartJpg.Add(fi.FullName);
            return actionList;
        }

        public sealed override void Reset()
        {
            _doneFanartJpg = new List<string>(); 
        }
    }
}
