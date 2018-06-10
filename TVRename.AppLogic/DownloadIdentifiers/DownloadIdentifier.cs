using System.IO;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;

namespace TVRename.AppLogic.DownloadIdentifiers
{
    public abstract class DownloadIdentifier
    {
        public abstract DownloadType GetDownloadType();

        public virtual ItemList ProcessSeries(ProcessedSeries si, bool forceRefresh = false)
        {
            return null;
        }

        public virtual ItemList ProcessSeason(ProcessedSeries si, string folder, int snum, bool forceRefresh = false)
        {
            return null;
        }

        public virtual ItemList ProcessEpisode(ProcessedEpisode dbep, FileInfo filo, bool forceRefresh = false)
        {
            return null;
        }

        public virtual void NotifyComplete(FileInfo file)
        {
        }

        public abstract void Reset();
    }
}
