using System;
using System.Collections.Generic;
using System.IO;
using TVRename.AppLogic.Extensions;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.ScanItems.Actions;
using TVRename.AppLogic.Settings;

namespace TVRename.AppLogic.DownloadIdentifiers
{
    public sealed class IncorrectFileDates : DownloadIdentifier
    {
        private List<string> _doneFilesAndFolders;

        public IncorrectFileDates()
        {
            Reset();
        }

        public override DownloadType GetDownloadType() => DownloadType.DownloadMetaData;

        public override ItemList ProcessSeries(ProcessedSeries si, bool forceRefresh = false)
        {
            DateTime? newUpdateTime = si.TheSeries().LastAiredDate();
            if (ApplicationSettings.Instance.CorrectFileDates && newUpdateTime.HasValue)
            {
                //Any series before 1980 will get 1980 as the timestamp
                if (newUpdateTime.Value.CompareTo(DateTimeExtensions.WindowsStartDateTime) < 0)
                {
                    newUpdateTime = DateTimeExtensions.WindowsStartDateTime;
                }

                DirectoryInfo di = new DirectoryInfo(si.AutoAdd_FolderBase);
                if ((di.LastWriteTimeUtc != newUpdateTime.Value)&&(!_doneFilesAndFolders.Contains(di.FullName)))
                {
                    _doneFilesAndFolders.Add(di.FullName);
                    return new ItemList
                    {
                        new TouchFileAction(di, si, newUpdateTime.Value)
                    };
                }
            }
            return null;
        }

        public override ItemList ProcessSeason(ProcessedSeries si, string folder, int snum, bool forceRefresh = false)
        {
            DateTime? newUpdateTime = si.GetSeason(snum).LastAiredDate();

            if (ApplicationSettings.Instance.CorrectFileDates && newUpdateTime.HasValue)
            {
                //Any series before 1980 will get 1980 as the timestamp
                if (newUpdateTime.Value.CompareTo(DateTimeExtensions.WindowsStartDateTime) < 0)
                {
                    newUpdateTime = DateTimeExtensions.WindowsStartDateTime;
                }

                DirectoryInfo di = new DirectoryInfo(folder);
                if ((di.LastWriteTimeUtc != newUpdateTime.Value) &&(!_doneFilesAndFolders.Contains(di.FullName)))
                {
                    _doneFilesAndFolders.Add(di.FullName);
                    return new ItemList() { new TouchFileAction(di, si, newUpdateTime.Value) };
                }
                
            }
            return null;
        }

        public override ItemList ProcessEpisode(ProcessedEpisode dbep, FileInfo filo, bool forceRefresh = false)
        {
            if (ApplicationSettings.Instance.CorrectFileDates && dbep.FirstAired.HasValue)
            {
                DateTime newUpdateTime = dbep.FirstAired.Value;

                //Any series before 1980 will get 1980 as the timestamp
                if (newUpdateTime.CompareTo(DateTimeExtensions.WindowsStartDateTime) < 0)
                {
                    newUpdateTime = DateTimeExtensions.WindowsStartDateTime;
                }

                if (filo.LastWriteTimeUtc != newUpdateTime && !_doneFilesAndFolders.Contains(filo.FullName))
                {
                    _doneFilesAndFolders.Add(filo.FullName);
                    return new ItemList
                    {
                        new TouchFileAction(filo,dbep, newUpdateTime)
                    };
                }
            }

            return null;
        }
        public override void Reset()
        {
            _doneFilesAndFolders = new List<string>();
        }

    }
}
