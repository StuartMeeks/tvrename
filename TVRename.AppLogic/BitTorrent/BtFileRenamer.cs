using System.IO;
using TVRename.AppLogic.Delegates;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.ScanItems.Actions;

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
            if (onDisk.Directory == null)
            {
                return true;
            }

            var newItem = new CopyMoveRenameFileAction(onDisk,
                FileHelper.FileInFolder(CopyNotMove ? CopyToFolder : onDisk.Directory.Name, nameInTorrent),
                CopyNotMove ? FileOperationType.Copy : FileOperationType.Rename, null, null, null);

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
            var r = "Hash Cache: " + CacheItems + " items for " + HashCache.Count + " files.  " + CacheHits + " hits from " + CacheChecks + " lookups";
            if (CacheChecks != 0)
            {
                r += " (" + (100 * CacheHits / CacheChecks) + "%)";
            }

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

            var r = ProcessTorrentFile(torrentFile); //TODO: Put this back, tvTree, args);

            return r;
        }
    }
}
