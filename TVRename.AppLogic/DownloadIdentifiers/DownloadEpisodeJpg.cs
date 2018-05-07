using System.Collections.Generic;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.Settings;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;


namespace TVRename.AppLogic.DownloadIdentifiers
{
    public class DownloadEpisodeJpg : DownloadIdentifier
    {
        private List<string> _doneJpg;
        private const string DefaultExtension = ".jpg";

        public DownloadEpisodeJpg() 
        {
            Reset();
        }

        public override DownloadType GetDownloadType() => DownloadType.DownloadImage;

        public override ItemList ProcessEpisode(ProcessedEpisode dbep, FileInfo filo, bool forceRefresh)
        {
            if (ApplicationSettings.Instance.EpJPGs)
            {
                ItemList actionList = new ItemList(); 
                string ban = dbep.GetFilename();
                if (!string.IsNullOrEmpty(ban))
                {
                    string basefn = filo.RemoveExtension();

                    FileInfo imgjpg = FileHelper.FileInFolder(filo.Directory, basefn + DefaultExtension);

                    if (forceRefresh || !imgjpg.Exists)
                        actionList.Add(new DownloadImageActionItem(dbep.SI, dbep, imgjpg, ban, ApplicationSettings.Instance.ShrinkLargeMede8erImages));
                }

                return actionList;
            }

            return base.ProcessEpisode(dbep, filo, forceRefresh);
        }

        public sealed override void Reset()
        {
            _doneJpg = new List<string>();
        }
    }

}
