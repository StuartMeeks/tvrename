using System;
using Alphaleonis.Win32.Filesystem;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.Sab;

namespace TVRename.AppLogic.ScanItems
{
    public class SabNzbDItem : InProgressItem
    {
        public queueSlotsSlot Entry;

        public override string ItemTargetFolder => string.IsNullOrEmpty(Entry.filename) ? null : new FileInfo(Entry.filename).DirectoryName;
        public override int ItemIconNumber => 8;

        public SabNzbDItem(queueSlotsSlot qss, ProcessedEpisode processedEpisode, string desiredLocationNoExt)
        {
            ItemEpisode = processedEpisode;
            DesiredLocationNoExt = desiredLocationNoExt;
            Entry = qss;
        }

        public override bool Equals(ItemBase other)
        {
            if (other is SabNzbDItem realOther)
            {
                return Entry == realOther.Entry;
            }

            return false;
        }

        public override int CompareTo(object other)
        {
            if (other == null)
            {
                return 1;
            }

            if (other.GetType() != typeof(SabNzbDItem))
            {
                return 1;
            }

            return CompareTo(other as SabNzbDItem);
        }

        public override int CompareTo(ItemBase other)
        {
            if (other is SabNzbDItem realOther)
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
