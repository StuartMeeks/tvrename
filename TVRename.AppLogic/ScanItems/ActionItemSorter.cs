using System;
using System.Collections.Generic;
using System.Text;

namespace TVRename.AppLogic.ScanItems
{
    public class ActionItemSorter : System.Collections.Generic.IComparer<ItemBase>
    {
        public virtual int Compare(ItemBase x, ItemBase y)
        {
            return (x.GetType() == y.GetType()) ? x.CompareTo(y) : (TypeNumber(x) - TypeNumber(y));
        }

        private static int TypeNumber(ItemBase item)
        {
            if (item is MissingItem)
                return 1;
            if (item is CopyMoveRenameActionItem)
                return 2;
            if (item is ActionRSS)
                return 3;
            if (item is DownloadImageActionItem)
                return 4;
            if (item is ActionNFO)
                return 5;
            if (item is ItemuTorrenting || item is ItemSABnzbd)
                return 6;
            return 7;
        }
    }
}
