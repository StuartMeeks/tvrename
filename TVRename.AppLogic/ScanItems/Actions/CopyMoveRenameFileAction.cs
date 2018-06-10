using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.Native;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems.Items;
using TVRename.AppLogic.Settings;

namespace TVRename.AppLogic.ScanItems.Actions
{
    public class CopyMoveRenameFileAction : FileOperationAction
    {
        // Local members
        public FileInfo SourceFile { get; }
        public FileInfo TargetFile { get; }
        public FileOperationType Operation { get; set; }
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
        public CopyMoveRenameFileAction(FileInfo sourceFile, FileInfo targetFile, FileOperationType operation, ProcessedEpisode processedEpisode,
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
            if (other is CopyMoveRenameFileAction realOther)
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

            if (other.GetType() != typeof(CopyMoveRenameFileAction))
            {
                return 1;
            }

            return CompareTo(other as CopyMoveRenameFileAction);
        }

        public override int CompareTo(ItemBase other)
        {
            if (other is CopyMoveRenameFileAction realOther)
            {
                if (SourceFile.Directory == null
                    || TargetFile.Directory == null
                    || realOther.SourceFile == null
                    || realOther.TargetFile == null)
                {
                    return 1;
                }

                var s1 = SourceFile.FullName + (SourceFile.Directory.Root.FullName != TargetFile.Directory.Root.FullName ? "0" : "1");
                var s2 = realOther.SourceFile.FullName +
                         (realOther.TargetFile.Directory != null && realOther.SourceFile.Directory != null && realOther.SourceFile.Directory.Root.FullName !=
                          realOther.TargetFile.Directory.Root.FullName
                             ? "0"
                             : "1");

                return string.Compare(s1, s2, StringComparison.Ordinal);
            }

            return 1;
        }

        private bool IsMoveRename() => Operation == FileOperationType.Move || Operation == FileOperationType.Rename;

        public bool IsSameSource(CopyMoveRenameFileAction other) => FileHelper.IsSameFile(SourceFile, other.SourceFile);

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
                case FileOperationType.Rename:
                    return "lvgActionRename";
                case FileOperationType.Copy:
                    return "lvgActionCopy";
                case FileOperationType.Move:
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

        private void CopyProgressCallback(ProgressChangedEventArgs args)
        {
            PercentDone = args.ProgressPercentage > 100 ? 100 : args.ProgressPercentage;
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
                var tempName = GetTempFilenameFor(TargetFile);

                // If both full filenames are the same then we want to move it away and back
                //This deals with an issue on some systems (XP?) that case insensitive moves did not occur
                if (IsMoveRename() || FileHelper.IsSameFile(SourceFile, TargetFile))
                {
                    SourceFile.MoveTo(TargetFile.FullName);

                    // This step could be slow, so report progress
                    FileOperations.Move(SourceFile.FullName, tempName, true, (sender, args) =>
                        {
                            CopyProgressCallback(args);
                        });
                }
                else
                {
                    //we are copying
                    Debug.Assert(Operation == FileOperationType.Copy);

                    // This step could be slow, so report progress
                    FileOperations.Copy(SourceFile.FullName, tempName, true, true, (sender, args) =>
                        {
                            CopyProgressCallback(args);
                        });
                }

                // Copying the temp file into the correct name is very quick, so no progress reporting		
                FileOperations.Move(tempName, TargetFile.FullName, true);

                // TODO: Put this back
                // logger.Info($"{this.Name} completed: {this.From.FullName} to {this.To.FullName } ");
                ActionCompleted = true;

                switch (Operation)
                {
                    case FileOperationType.Move:
                        stats.FilesMoved++;
                        break;
                    case FileOperationType.Rename:
                        stats.FilesRenamed++;
                        break;
                    case FileOperationType.Copy:
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
                if (Operation == FileOperationType.Move && Tidyup != null && Tidyup.DeleteEmpty)
                {
                    // TODO: Put this back
                    // logger.Info($"Testing {this.From.Directory.FullName} to see whether it should be tidied up");
                    DoTidyup(SourceFile.Directory);
                }
            }
            catch (Exception e)
            {
                ActionCompleted = true;
                ActionError = true;
                ActionErrorText = e.Message;
            }

            return !ActionError;
        }


    }
}
