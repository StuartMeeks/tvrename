namespace TVRename.AppLogic.ScanItems
{
    public abstract class DeleteActionItem : FileOperationActionItem
    {
        public override string ActionName => "Delete";
        public override long ActionSizeOfWork => 100;
        public override int ItemIconNumber => 9;
        public override string ItemGroup => "lvgActionDelete";
    }
}
