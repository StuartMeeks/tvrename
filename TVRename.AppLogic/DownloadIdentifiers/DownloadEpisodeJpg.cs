using System.Collections.Generic;
using System.IO;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.ScanItems.Actions;
using TVRename.AppLogic.Settings;


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

        public override ItemList ProcessEpisode(ProcessedEpisode dbep, FileInfo filo, bool forceRefresh = false)
        {
            if (!ApplicationSettings.Instance.EpJPGs)
            {
                return base.ProcessEpisode(dbep, filo, forceRefresh);
            }

            var actionList = new ItemList(); 
            var ban = dbep.GetFilename();
            if (string.IsNullOrEmpty(ban))
            {
                return actionList;
            }

            var basefn = filo.RemoveExtension();
            var imgjpg = FileHelper.FileInFolder(filo.Directory, basefn + DefaultExtension);

            if (forceRefresh || !imgjpg.Exists)
            {
                actionList.Add(new DownloadImageAction(dbep.SI, dbep, imgjpg, ban, ApplicationSettings.Instance.ShrinkLargeMede8erImages));
            }

            return actionList;

        }

        public sealed override void Reset()
        {
            _doneJpg = new List<string>();
        }
    }

}
