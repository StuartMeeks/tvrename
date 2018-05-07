using System.Collections.Generic;
using TVRename.AppLogic.ScanItems;

namespace TVRename.AppLogic
{
    public class ItemList : List<ItemBase>
    {
        public void Add(ItemList itemList)
        {
            if (itemList == null)
            {
                return;
            }

            foreach (ItemBase itemBase in itemList)
            {
                Add(itemBase);
            }
        }
    }
}
