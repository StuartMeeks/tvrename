using Alphaleonis.Win32.Filesystem;
using TVRename.AppLogic.Delegates;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ScanItems;

namespace TVRename.AppLogic.BitTorrent
{
    public class BtFileRenamer : BtCore
    {
        public bool CopyNotMove;
        public string CopyToFolder;
        public ItemList RenameListOut;

        public BtFileRenamer(ProgressUpdatedDelegate progressUpdatedDelegate)
            : base(progressUpdatedDelegate)
        {
        }

        public override bool NewTorrentEntry(string torrentFile, int numberInTorrent)
        {
            return true;
        }

        public override bool FoundFileOnDiskForFileInTorrent(string torrentFile, FileInfo onDisk, int numberInTorrent, string nameInTorrent)
        {
            var newItem = new CopyMoveRenameActionItem(onDisk,
                FileHelper.FileInFolder(this.CopyNotMove ? this.CopyToFolder : onDisk.Directory.Name, nameInTorrent),
                CopyNotMove ? FileOperation.Copy : FileOperation.Rename, null, null, null);

            RenameListOut.Add(newItem);

            return true;
        }

        public override bool DidNotFindFileOnDiskForFileInTorrent(string torrentFile, int numberInTorrent, string nameInTorrent)
        {
            return true;
        }

        public override bool FinishedTorrentEntry(string torrentFile, int numberInTorrent, string filename)
        {
            return true;
        }

        public string CacheStats()
        {
            string r = "Hash Cache: " + this.CacheItems + " items for " + this.HashCache.Count + " files.  " + this.CacheHits + " hits from " + this.CacheChecks + " lookups";
            if (this.CacheChecks != 0)
                r += " (" + (100 * this.CacheHits / this.CacheChecks) + "%)";
            return r;
        }

        public bool RenameFilesOnDiskToMatchTorrent(string torrentFile, string folder,
            ItemList renameListOut, bool copyNotMove, string copyDest) // TODO: Put this back, TreeView tvTree)
        {
            if ((string.IsNullOrEmpty(folder) || !Directory.Exists(folder)))
                return false;

            if (string.IsNullOrEmpty(torrentFile))
                return false;

            if (renameListOut == null)
                return false;

            if (copyNotMove && (string.IsNullOrEmpty(copyDest) || !Directory.Exists(copyDest)))
                return false;

            CopyNotMove = copyNotMove;
            CopyToFolder = copyDest;
            DoHashChecking = true;
            RenameListOut = renameListOut;

            Prog(0);

            BuildFileCache(folder, false); // don't do subfolders

            RenameListOut.Clear();

            bool r = this.ProcessTorrentFile(torrentFile); //TODO: Put this back, tvTree, args);

            return r;
        }
    }
}
