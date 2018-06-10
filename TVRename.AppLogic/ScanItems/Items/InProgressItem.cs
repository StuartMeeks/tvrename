namespace TVRename.AppLogic.ScanItems.Items
{
    public abstract class InProgressItem : ItemBase
    {
        public string DesiredLocationNoExt;

        public override string ItemGroup => "lvgDownloading";

        public override IgnoreItem ItemIgnore => string.IsNullOrEmpty(DesiredLocationNoExt) ? null : new IgnoreItem(DesiredLocationNoExt);
    }
}
