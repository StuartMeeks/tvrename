using System.Collections.Generic;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.Settings;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;

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

        public override ItemList ProcessShow(ProcessedSeries si, bool forceRefresh)
        {
            //We only want to do something if the fanart option is enabled. If the KODI option is enabled then let it do the work.
            if ((ApplicationSettings.Instance.FanArtJpg) && !ApplicationSettings.Instance.KODIImages)
            {
                ItemList actionList = new ItemList();
                FileInfo fi = FileHelper.FileInFolder(si.AutoAdd_FolderBase, DefaultFileName);

                bool doesntExist =  !fi.Exists;
                if ((forceRefresh ||doesntExist) &&(!_doneFanartJpg.Contains(fi.FullName)))
                {
                    string bannerPath = si.TheSeries().GetSeriesFanartPath();

                    if (!string.IsNullOrEmpty(bannerPath))
                    {
                        actionList.Add(new DownloadImageActionItem(si, null, fi, bannerPath, false));
                    }
                    _doneFanartJpg.Add(fi.FullName);
                }
                return actionList;

            }
            return base.ProcessShow(si, forceRefresh);
        }

        public sealed override void Reset()
        {
            _doneFanartJpg = new List<string>(); 
        }
    }
}
