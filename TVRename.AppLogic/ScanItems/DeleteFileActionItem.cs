using System;
using Alphaleonis.Win32.Filesystem;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.Settings;

namespace TVRename.AppLogic.ScanItems
{
    public class DeleteFileActionItem : DeleteActionItem
    {
        private readonly FileInfo _fileToRemove;

        public override string ActionProgressText => _fileToRemove.Name;
        public override string ActionProduces => _fileToRemove.FullName;
        public override IgnoreItem ItemIgnore => _fileToRemove == null ? null : new IgnoreItem(_fileToRemove.FullName);
        public override string ItemTargetFolder => _fileToRemove?.DirectoryName;

        public DeleteFileActionItem(FileInfo fileToRemove, ProcessedEpisode ep, TidySettings tidyup)
        {
            Tidyup = tidyup;
            PercentDone = 0;
            ItemEpisode = ep;
            _fileToRemove = fileToRemove;
        }

        public bool SameSource(DeleteFileActionItem other) => FileHelper.IsSameFile(_fileToRemove, other._fileToRemove);

        public override bool PerformAction(ref bool pause, Statistics stats)
        {
            try
            {
                if (_fileToRemove.Exists)
                {
                    DeleteOrRecycleFile(_fileToRemove);
                    if (Tidyup != null && Tidyup.DeleteEmpty)
                    {
                        // TODO: Put this back:
                        //logger.Info($"Testing {this.toRemove.Directory.FullName } to see whether it should be tidied up");
                        DoTidyup(_fileToRemove.Directory);
                    }
                }
            }
            catch (Exception e)
            {
                ActionError = true;
                ActionErrorText = e.Message;
            }

            ActionCompleted = true;
            return !ActionError;
        }

        public override bool Equals(ItemBase other)
        {
            if (other is DeleteFileActionItem realOther)
            {
                return FileHelper.IsSameFile(_fileToRemove, realOther._fileToRemove);
            }

            return false;
        }

        public override int CompareTo(object other)
        {
            if (other == null)
            {
                return 1;
            }

            if (other.GetType() != typeof(DeleteFileActionItem))
            {
                return 1;
            }

            return CompareTo(other as DeleteFileActionItem);
        }

        public override int CompareTo(ItemBase other)
        {
            if (other is DeleteFileActionItem realOther)
            {
                return string.Compare(_fileToRemove.FullName, realOther._fileToRemove.FullName, StringComparison.Ordinal);
            }

            return 0;
        }
    }
}
