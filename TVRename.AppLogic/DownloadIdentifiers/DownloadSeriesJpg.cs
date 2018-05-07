using System.Collections.Generic;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.Settings;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;

namespace TVRename.AppLogic.DownloadIdentifiers
{
    public sealed class DownloadSeriesJpg : DownloadIdentifier
    {
        private List<string> _doneJpg;
        private const string DefaultFileName = "series.jpg";

        public DownloadSeriesJpg() 
        {
            Reset();
        }

        public override DownloadType GetDownloadType() => DownloadType.DownloadImage;

        public override ItemList ProcessSeason(ProcessedSeries si, string folder, int snum, bool forceRefresh)
        {
            if (ApplicationSettings.Instance.SeriesJpg)
            {
                ItemList actionList = new ItemList();
                FileInfo fi = FileHelper.FileInFolder(folder, DefaultFileName);
                if (forceRefresh ||(!_doneJpg.Contains(fi.FullName) && !fi.Exists))
                {
                    string bannerPath = si.TheSeries().GetSeasonBannerPath(snum);
                    if (!string.IsNullOrEmpty(bannerPath))
                    {
                        actionList.Add(new DownloadImageActionItem(si, null, fi, bannerPath, ApplicationSettings.Instance.ShrinkLargeMede8erImages));
                    }

                    _doneJpg.Add(fi.FullName);
                }
                return actionList;
            }


            return base.ProcessSeason(si, folder, snum, forceRefresh);
        }

        public override void Reset()
        {
            _doneJpg  = new List<string>();
        }
    }
}
