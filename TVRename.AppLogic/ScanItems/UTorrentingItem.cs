using System;
using Alphaleonis.Win32.Filesystem;
using TVRename.AppLogic.BitTorrent;
using TVRename.AppLogic.ProcessedItems;

namespace TVRename.AppLogic.ScanItems
{
    public class UTorrentingItem : InProgressItem
    {
        public TorrentEntry TorrentEntry;

        public override string ItemTargetFolder => string.IsNullOrEmpty(TorrentEntry.DownloadingTo) ? null : new FileInfo(TorrentEntry.DownloadingTo).DirectoryName;
        public override int ItemIconNumber => 2;

        public UTorrentingItem(TorrentEntry torrentEntry, ProcessedEpisode processedEpisode, string desiredLocationNoExt)
        {
            TorrentEntry = torrentEntry;
            ItemEpisode = processedEpisode;
            DesiredLocationNoExt = desiredLocationNoExt;
        }

        public override bool Equals(ItemBase other)
        {
            if (other is UTorrentingItem realOther)
            {
                return TorrentEntry == realOther.TorrentEntry;
            }

            return false;
        }

        public override int CompareTo(object other)
        {
            if (other == null)
            {
                return 1;
            }

            if (other.GetType() != typeof(UTorrentingItem))
            {
                return 1;
            }

            return CompareTo(other as UTorrentingItem);
        }

        public override int CompareTo(ItemBase other)
        {
            if (other is UTorrentingItem realOther)
            {
                if (ItemEpisode == null)
                {
                    return 1;
                }

                if (realOther.ItemEpisode == null)
                {
                    return -1;
                }

                return string.Compare(DesiredLocationNoExt, realOther.DesiredLocationNoExt, StringComparison.Ordinal);
            }

            return 0;
        }
    }
}
