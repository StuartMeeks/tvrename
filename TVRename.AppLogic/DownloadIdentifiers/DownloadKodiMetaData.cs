using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;
using TVRename.AppLogic.ScanItems.Actions;
using TVRename.AppLogic.Settings;

namespace TVRename.AppLogic.DownloadIdentifiers
{
    public class DownloadKodiMetaData : DownloadIdentifier
    {
        private static List<string> _doneNfo;

        public DownloadKodiMetaData()
        {
            Reset();
        }

        public override DownloadType GetDownloadType()
        {
            return DownloadType.DownloadMetaData;
        }

        public override void NotifyComplete(FileInfo file)
        {
            if (file.FullName.EndsWith(".nfo", true, new CultureInfo("en"))) _doneNfo.Add(file.FullName);
            base.NotifyComplete(file);
        }

        public override ItemList ProcessSeries(ProcessedSeries si, bool forceRefresh = false)
        {
            // for each tv show, optionally write a tvshow.nfo file
            if (ApplicationSettings.Instance.NFOShows)
            {
                var actionList = new ItemList();
                var tvshownfo = FileHelper.FileInFolder(si.AutoAdd_FolderBase, "tvshow.nfo");

                var needUpdate = !tvshownfo.Exists ||
                                 si.TheSeries().Srv_LastUpdated > TimeZoneHelper.Epoch(tvshownfo.LastWriteTime) ||
                                 tvshownfo.LastWriteTime.ToUniversalTime()
                                     .CompareTo(new DateTime(2009, 9, 13, 7, 30, 0, 0, DateTimeKind.Utc)) < 0;

                var alreadyOnTheList = _doneNfo.Contains(tvshownfo.FullName);

                if ((forceRefresh || needUpdate) && !alreadyOnTheList)
                {
                    actionList.Add(new NfoAction(tvshownfo, si));
                    _doneNfo.Add(tvshownfo.FullName);
                }

                return actionList;
            }

            return base.ProcessSeries(si, forceRefresh);
        }

        public override ItemList ProcessEpisode(ProcessedEpisode dbep, FileInfo filo, bool forceRefresh = false)
        {
            if (ApplicationSettings.Instance.NFOEpisodes)
            {
                var actionList = new ItemList();

                var fn = filo.RemoveExtension() + ".nfo";
                var nfo = FileHelper.FileInFolder(filo.Directory, fn);

                if (!nfo.Exists || dbep.Srv_LastUpdated > TimeZoneHelper.Epoch(nfo.LastWriteTime) || forceRefresh)
                {
                    if (!_doneNfo.Contains(nfo.FullName))
                    {
                        actionList.Add(new NfoAction(nfo, dbep));
                        _doneNfo.Add(nfo.FullName);
                    }
                }

                return actionList;
            }

            return base.ProcessEpisode(dbep, filo, forceRefresh);
        }

        public sealed override void Reset()
        {
            _doneNfo = new List<string>();
        }
    }
}
