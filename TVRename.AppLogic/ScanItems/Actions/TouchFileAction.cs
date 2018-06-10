using System;
using System.IO;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems.Items;
using TVRename.AppLogic.TheTvDb;

namespace TVRename.AppLogic.ScanItems.Actions
{
    public class TouchFileAction : FileMetaDataAction
    {
        public ProcessedSeries ProcessedSeries;
        public TheTvDbSeason Season;

        public FileInfo FileToTouch;
        public DirectoryInfo DirectoryToTouch;

        private readonly DateTime _updateTime;

        public override string ActionName => "Update Timestamp";
        public override string ActionProgressText => FileToTouch?.Name ?? DirectoryToTouch?.Name;
        public override long ActionSizeOfWork => 100;
        public override string ActionProduces => FileToTouch?.FullName ?? DirectoryToTouch?.FullName;

        public override IgnoreItem ItemIgnore => FileToTouch == null ? null : new IgnoreItem(FileToTouch.FullName);
        public override string ItemTargetFolder => this.FileToTouch?.DirectoryName ?? this.DirectoryToTouch?.Name;
        public override string ItemGroup => "lvgUpdateFileDates";
        public override int ItemIconNumber => 7;


        public TouchFileAction(FileInfo fileToTouch, ProcessedEpisode processedEpisode, DateTime date)
        {
            FileToTouch = fileToTouch;
            ItemEpisode = processedEpisode;
            _updateTime = date;
        }

        public TouchFileAction(DirectoryInfo directoryToTouch, TheTvDbSeason season, DateTime date)
        {
            DirectoryToTouch = directoryToTouch;
            Season = season;
            _updateTime = date;

        }

        public TouchFileAction(DirectoryInfo directoryToTouch, ProcessedSeries processedSeries, DateTime date)
        {
            ProcessedSeries = processedSeries;
            DirectoryToTouch = directoryToTouch;
            _updateTime = date;
        }


        public override bool PerformAction(ref bool pause, Statistics stats)
        {
            try
            {
                if (FileToTouch != null)
                {
                    bool priorFileReadonly = FileToTouch.IsReadOnly;

                    if (priorFileReadonly) FileToTouch.IsReadOnly = false;
                    File.SetLastWriteTimeUtc(FileToTouch.FullName, _updateTime);
                    if (priorFileReadonly) FileToTouch.IsReadOnly = true;

                }

                if (DirectoryToTouch != null)
                {
                    Directory.SetLastWriteTimeUtc(DirectoryToTouch.FullName, _updateTime);
                }
            }
            catch (Exception e)
            {
                ActionErrorText = e.Message;
                ActionError = true;
                ActionCompleted = true;

                return false;
            }

            ActionCompleted = true;

            return true;
        }

        public override bool Equals(ItemBase other)
        {
            if (other is TouchFileAction realOther)
            {
                return FileToTouch == realOther.FileToTouch
                       && DirectoryToTouch == realOther.DirectoryToTouch;
            }

            return false;
        }

        public override int CompareTo(object other)
        {
            if (other == null)
            {
                return 1;
            }

            if (other.GetType() != typeof(TouchFileAction))
            {
                return 1;
            }

            return CompareTo(other as TouchFileAction);
        }

        public override int CompareTo(ItemBase other)
        {
            if (other is TouchFileAction realOther)
            {
                if (ItemEpisode == null)
                {
                    return 1;
                }

                if (realOther.ItemEpisode == null)
                {
                    return -1;
                }

                if (FileToTouch != null)
                {
                    return string.Compare(FileToTouch.FullName + ItemEpisode.Name,
                        realOther.FileToTouch.FullName + realOther.ItemEpisode.Name, StringComparison.Ordinal);
                }

                if (DirectoryToTouch != null)
                {
                    return string.Compare(DirectoryToTouch.FullName + ItemEpisode.Name,
                        realOther.DirectoryToTouch.FullName + realOther.ItemEpisode.Name, StringComparison.Ordinal);
                }
            }

            return 1;
        }
    }
}
