using System.Collections.Generic;
using TVRename.AppLogic.ScanItems.Actions;
using TVRename.AppLogic.ScanItems.Items;

namespace TVRename.AppLogic.ScanItems
{
    public class ItemSorter : IComparer<ItemBase>
    {
        public virtual int Compare(ItemBase x, ItemBase y)
        {
            return (x.GetType() == y.GetType()) ? x.CompareTo(y) : (TypeNumber(x) - TypeNumber(y));
        }

        private static int TypeNumber(ItemBase item)
        {
            if (item is MissingItem)
                return 1;
            if (item is CopyMoveRenameFileAction)
                return 2;
            if (item is RssAction)
                return 3;
            if (item is DownloadImageAction)
                return 4;
            if (item is NfoAction)
                return 5;
            if (item is UTorrentingItem || item is SabNzbDItem)
                return 6;
            return 7;
        }
    }
}
