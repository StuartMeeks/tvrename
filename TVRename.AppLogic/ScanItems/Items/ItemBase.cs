using System;
using TVRename.AppLogic.ProcessedItems;

namespace TVRename.AppLogic.ScanItems.Items
{
    /// <summary>
    /// An item shown in the list on the Scan tab (not necessarily always with an action)
    /// </summary>
    public abstract class ItemBase : IComparable, IComparable<ItemBase>, IEquatable<ItemBase>
    {
        /// <summary>
        /// Returns a list of folders for the right-click menu
        /// </summary>
        public abstract string ItemTargetFolder { get; }

        /// <summary>
        /// The name of the group for the listview
        /// </summary>
        public abstract string ItemGroup { get; }

        /// <summary>
        /// Which icon to use
        /// </summary>
        public abstract int ItemIconNumber { get; }

        /// <summary>
        /// What to add to the ignore list / compare against the ignore list
        /// </summary>
        public abstract IgnoreItem ItemIgnore { get; }

        /// <summary>
        /// The episode associated with this item
        /// </summary>
        public ProcessedEpisode ItemEpisode { get; protected set; }

        public abstract int CompareTo(object obj);
        public abstract int CompareTo(ItemBase other);
        public abstract bool Equals(ItemBase other);
    }
}
