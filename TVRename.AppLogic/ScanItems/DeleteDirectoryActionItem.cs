using Alphaleonis.Win32.Filesystem;
using System;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.Settings;

namespace TVRename.AppLogic.ScanItems
{
    public class DeleteDirectoryActionItem : DeleteActionItem
    {

        private readonly DirectoryInfo _directoryToRemove;

        public override string ActionProgressText => _directoryToRemove.Name;
        public override string ActionProduces => _directoryToRemove.FullName;
        public override IgnoreItem ItemIgnore => _directoryToRemove == null ? null : new IgnoreItem(_directoryToRemove.FullName);
        public override string ItemTargetFolder => _directoryToRemove?.Parent.FullName;

        public DeleteDirectoryActionItem(DirectoryInfo directoryToRemove, ProcessedEpisode ep, TidySettings tidyup)
        {
            Tidyup = tidyup;
            PercentDone = 0;
            ItemEpisode = ep;
            _directoryToRemove = directoryToRemove;
        }

        public bool SameSource(DeleteDirectoryActionItem other) => FileHelper.IsSameDirectory(_directoryToRemove, other._directoryToRemove);

        public override bool PerformAction(ref bool pause, Statistics stats)
        {
            //if the directory is the root download folder do not delete
            if (ApplicationSettings.Instance.MonitorFolders &&
                ApplicationSettings.Instance.DownloadFoldersNames.Contains(_directoryToRemove.FullName))
            {
                ActionError = true;
                ActionErrorText = $@"Not removing {_directoryToRemove.FullName} as it is a Search Folder";

                return false;
            }

            try
            {
                if (_directoryToRemove.Exists)
                {
                    DeleteOrRecycleFolder(_directoryToRemove);
                    if (Tidyup != null && Tidyup.DeleteEmpty)
                    {
                        // TODO: Put this back
                        // logger.Info($"Testing {this.toRemove.Parent.FullName } to see whether it should be tidied up");
                        DoTidyup(_directoryToRemove.Parent);
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
            if (other is DeleteDirectoryActionItem realOther)
            {
                return FileHelper.IsSameDirectory(_directoryToRemove, realOther._directoryToRemove);
            }

            return false;
        }

        public override int CompareTo(object other)
        {
            if (other == null)
            {
                return 1;
            }

            if (other.GetType() != typeof(DeleteDirectoryActionItem))
            {
                return 1;
            }

            return CompareTo(other as DeleteDirectoryActionItem);
        }

        public override int CompareTo(ItemBase other)
        {
            if (other is DeleteDirectoryActionItem realOther)
            {

                return string.Compare(_directoryToRemove.FullName, realOther._directoryToRemove.FullName, StringComparison.Ordinal);

            }

            return 0;
        }
    }
}
