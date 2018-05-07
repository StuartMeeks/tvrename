using System;
using Alphaleonis.Win32.Filesystem;
using TVRename.AppLogic.ProcessedItems;

namespace TVRename.AppLogic.ScanItems
{
    public class MissingItem : ItemBase
    {
        private string _folder;

        public string TheFileNoExt;
        public string Filename;

        public override string ItemGroup => "lvgActionMissing";

        public override IgnoreItem ItemIgnore => string.IsNullOrEmpty(this.TheFileNoExt) ? null : new IgnoreItem(this.TheFileNoExt);
        public override string ItemTargetFolder => string.IsNullOrEmpty(this.TheFileNoExt) ? null : new FileInfo(this.TheFileNoExt).DirectoryName;
        public override int ItemIconNumber => 1;

        public MissingItem(ProcessedEpisode processedEpisode, string targetFolder, string expectedFilenameNoExt)
        {
            ItemEpisode = processedEpisode;
            TheFileNoExt = targetFolder + Path.DirectorySeparatorChar + expectedFilenameNoExt;
            Filename = expectedFilenameNoExt;

            _folder = targetFolder;
        }

        public override bool Equals(ItemBase other)
        {
            if (other is MissingItem realOther)
            {
                return string.Compare(TheFileNoExt, realOther.TheFileNoExt, StringComparison.InvariantCultureIgnoreCase) == 0;
            }

            return false;
        }

        public override int CompareTo(object other)
        {
            if (other == null)
            {
                return 1;
            }

            if (other.GetType() != typeof(MissingItem))
            {
                return 1;
            }

            return CompareTo(other as MissingItem);
        }

        public override int CompareTo(ItemBase other)
        {
            if (other is MissingItem realOther)
            {
                if (!ItemEpisode.SI.ShowName.Equals(realOther.ItemEpisode.SI.ShowName))
                {
                    return ItemEpisode.SI.ShowName.CompareTo(realOther.ItemEpisode.SI.ShowName);
                }

                if (!ItemEpisode.AppropriateSeasonNumber.Equals(ItemEpisode.AppropriateSeasonNumber))
                {
                    return ItemEpisode.AppropriateSeasonNumber.CompareTo(realOther.ItemEpisode.AppropriateSeasonNumber);
                }

                return ItemEpisode.AppropriateEpNum.CompareTo(realOther.ItemEpisode.AppropriateEpNum);
            }

            return 0;
        }


    }
}
