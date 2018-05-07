using System;
using System.Diagnostics;
using Alphaleonis.Win32.Filesystem;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.Settings;

namespace TVRename.AppLogic.ScanItems
{
    public class CopyMoveRenameActionItem : FileOperationActionItem
    {
        // Local members
        public FileInfo SourceFile { get; }
        public FileInfo TargetFile { get; }
        public FileOperation Operation { get; set;  }
        public MissingItem UndoItemMissing { get; }

        // Overrides for ItemBase
        public override string ItemTargetFolder => TargetFile?.DirectoryName;
        public override string ItemGroup => GetItemGroup();
        public override int ItemIconNumber => IsMoveRename() ? 4 : 3;
        public override IgnoreItem ItemIgnore => TargetFile == null ? null : new IgnoreItem(TargetFile.FullName);

        // These overrides are for ActionItemBase
        public override string ActionName => IsMoveRename() ? "Move" : "Copy";
        public override string ActionProgressText => TargetFile.Name;
        public override string ActionProduces => TargetFile.FullName;
        public override long ActionSizeOfWork => IsQuickOperation() ? 10000 : SourceFileSize();


        //ctor
        public CopyMoveRenameActionItem(FileInfo sourceFile, FileInfo targetFile, FileOperation operation, ProcessedEpisode processedEpisode,
            TidySettings tidyup, MissingItem undoItem)
        {
            SourceFile = sourceFile;
            TargetFile = targetFile;
            Operation = operation;
            UndoItemMissing = undoItem;

            Tidyup = tidyup;
            PercentDone = 0;
            ItemEpisode = processedEpisode;
        }

        public override bool Equals(ItemBase other)
        {
            if (other is CopyMoveRenameActionItem realOther)
            {
                return Operation == realOther.Operation
                       && FileHelper.IsSameFile(SourceFile, realOther.SourceFile)
                       && FileHelper.IsSameFile(TargetFile, realOther.TargetFile);
            }

            return false;
        }

        public override int CompareTo(object other)
        {
            if (other == null)
            {
                return 1;
            }

            if (other.GetType() != typeof(CopyMoveRenameActionItem))
            {
                return 1;
            }

            return CompareTo(other as CopyMoveRenameActionItem);
        }

        public override int CompareTo(ItemBase other)
        {
            if (other is CopyMoveRenameActionItem realOther)
            {
                if (SourceFile.Directory == null
                    || TargetFile.Directory == null
                    || realOther.SourceFile == null
                    || realOther.TargetFile == null)
                {
                    return 1;
                }

                string s1 = SourceFile.FullName + (SourceFile.Directory.Root.FullName != TargetFile.Directory.Root.FullName ? "0" : "1");
                string s2 = realOther.SourceFile.FullName + (realOther.SourceFile.Directory.Root.FullName != realOther.TargetFile.Directory.Root.FullName ? "0" : "1");

                return string.Compare(s1, s2, StringComparison.Ordinal);
            }

            return 1;
        }

        public bool IsMoveRename() => Operation == FileOperation.Move || Operation == FileOperation.Rename;

        public bool IsSameSource(CopyMoveRenameActionItem other) => FileHelper.IsSameFile(SourceFile, other.SourceFile);
        
        public bool IsQuickOperation()
        {
            if (SourceFile == null
                || TargetFile == null
                || SourceFile.Directory == null
                || TargetFile.Directory == null)
            {
                return false;
            }

            return IsMoveRename() &&
                   string.Equals(SourceFile.Directory.Root.FullName, TargetFile.Directory.Root.FullName, StringComparison.InvariantCultureIgnoreCase);
            // TODO: Consider resolving UNC paths here to get further performance gains
        }

        private static string GetTempFilenameFor(FileSystemInfo f) => f.FullName + ".tvrenametemp";

        private string GetItemGroup()
        {
            switch (Operation)
            {
                case FileOperation.Rename:
                    return "lvgActionRename";
                case FileOperation.Copy:
                    return "lvgActionCopy";
                case FileOperation.Move:
                    return "lvgActionMove";
                default:
                    return "lvgActionCopy";
            }
        }

        private static void CopyTimestamps(FileSystemInfo source, FileSystemInfo target)
        {
            target.CreationTime = source.CreationTime;
            target.CreationTimeUtc = source.CreationTimeUtc;
            target.LastAccessTime = source.LastAccessTime;
            target.LastAccessTimeUtc = source.LastAccessTimeUtc;
            target.LastWriteTime = source.LastWriteTime;
            target.LastWriteTimeUtc = source.LastWriteTimeUtc;
        }

        private CopyMoveProgressResult CopyProgressCallback(long totalFileSize, long totalBytesTransferred, long StreamSize, long StreamBytesTransferred, int StreamNumber, CopyMoveProgressCallbackReason CallbackReason, Object UserData)
        {
            decimal percent = totalBytesTransferred * 100m / totalFileSize;
            PercentDone = percent > 100m ? 100m : percent;
            return CopyMoveProgressResult.Continue;
        }

        private long SourceFileSize()
        {
            try
            {
                return SourceFile.Length;
            }
            catch
            {
                return 1;
            }
        }

        public override bool PerformAction(ref bool pause, Statistics stats)
        {
            // TODO: Put this back
            // read NTFS permissions (if any)
            //FileSecurity security = null;
            //try
            //{
            //    security = SourceFile.GetAccessControl();
            //}
            //catch
            //{
            //    // ignored
            //}

            try
            {
                //we use a temp name just in case we are interruted or some other problem occurs
                string tempName = GetTempFilenameFor(TargetFile);

                // If both full filenames are the same then we want to move it away and back
                //This deals with an issue on some systems (XP?) that case insensitive moves did not occur
                if (IsMoveRename() || FileHelper.IsSameFile(SourceFile, TargetFile))
                {
                    // This step could be slow, so report progress
                    CopyMoveResult moveResult = File.Move(SourceFile.FullName, tempName, MoveOptions.CopyAllowed | MoveOptions.ReplaceExisting, CopyProgressCallback, null);
                    if (moveResult.ErrorCode != 0) throw new Exception(moveResult.ErrorMessage);
                }
                else
                {
                    //we are copying
                    Debug.Assert(Operation == FileOperation.Copy);

                    // This step could be slow, so report progress
                    CopyMoveResult copyResult = File.Copy(SourceFile.FullName, tempName, CopyOptions.None, true, CopyProgressCallback, null);
                    if (copyResult.ErrorCode != 0) throw new Exception(copyResult.ErrorMessage);
                }

                // Copying the temp file into the correct name is very quick, so no progress reporting		
                File.Move(tempName, TargetFile.FullName, MoveOptions.ReplaceExisting);

                // TODO: Put this back
                // logger.Info($"{this.Name} completed: {this.From.FullName} to {this.To.FullName } ");
                ActionCompleted = true;

                switch (Operation)
                {
                    case FileOperation.Move:
                        stats.FilesMoved++;
                        break;
                    case FileOperation.Rename:
                        stats.FilesRenamed++;
                        break;
                    case FileOperation.Copy:
                        stats.FilesCopied++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                ActionCompleted = true;
                ActionError = true;
                ActionErrorText = e.Message;
            }

            // TODO: Put this back
            // set NTFS permissions
            //try
            //{
            //    if (security != null) this.To.SetAccessControl(security);
            //}
            //catch
            //{
            //    // ignored
            //}

            try
            {
                if (Operation == FileOperation.Move && Tidyup != null && Tidyup.DeleteEmpty)
                {
                    // TODO: Put this back
                    // logger.Info($"Testing {this.From.Directory.FullName} to see whether it should be tidied up");
                    DoTidyup(SourceFile.Directory);
                }
            }
            catch (Exception e)
            {
                this.ActionCompleted = true;
                this.ActionError = true;
                this.ActionErrorText = e.Message;
            }

            return !this.ActionError;
        }


    }
}
