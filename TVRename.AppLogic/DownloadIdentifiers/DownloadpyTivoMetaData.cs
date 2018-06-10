using System.IO;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.Settings;

namespace TVRename.AppLogic.DownloadIdentifiers
{
    public class DownloadpyTivoMetaData : DownloadIdentifier
    {
        public DownloadpyTivoMetaData() 
        {
            Reset();
        }

        public override DownloadType GetDownloadType() => DownloadType.DownloadMetaData;

        public override ItemList ProcessEpisode(ProcessedEpisode dbep, FileInfo filo, bool forceRefresh)
        {
            if (ApplicationSettings.Instance.pyTivoMeta)
            {
                ItemList actionList = new ItemList(); 
                string fn = filo.Name;
                fn += ".txt";
                string folder = filo.DirectoryName;
                if (ApplicationSettings.Instance.pyTivoMetaSubFolder)
                {
                    folder += "\\.meta";
                }
                FileInfo meta = FileHelper.FileInFolder(folder, fn);

                if (!meta.Exists || (dbep.Srv_LastUpdated > TimeZoneHelper.Epoch(meta.LastWriteTime)))
                    actionList.Add(new ActionPyTivoMeta(meta, dbep));

                return actionList;
            }

            return base.ProcessEpisode(dbep, filo, forceRefresh);
        }

        public sealed override void Reset()
        {
        }
    }
}
