using Alphaleonis.Win32.Filesystem;
using TVRename.AppLogic.ProcessedItems;

namespace TVRename.AppLogic.DownloadIdentifiers
{
    public abstract class DownloadIdentifier
    {
        protected DownloadIdentifier()
        {
            
        }

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
