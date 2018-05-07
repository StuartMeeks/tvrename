using Alphaleonis.Win32.Filesystem;

namespace TVRename.AppLogic.ScanItems
{
    public abstract class WriteMetaDataActionItem : DownloadActionItem
    {
        public FileInfo FileToWrite;

        public override string ActionProduces => FileToWrite.FullName;

        public override string ActionProgressText => FileToWrite.Name;

        public override long ActionSizeOfWork => 10000;

        public override string ItemTargetFolder => FileToWrite == null ? null : FileToWrite.DirectoryName;

        public override IgnoreItem ItemIgnore => FileToWrite == null ? null : new IgnoreItem(FileToWrite.FullName);

        public override string ItemGroup => "lvgActionMeta";

        public override int ItemIconNumber => 7;
    }
}
