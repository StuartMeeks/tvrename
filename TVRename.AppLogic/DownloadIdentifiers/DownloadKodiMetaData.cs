using System;
using System.Collections.Generic;
using System.Globalization;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.Settings;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;

namespace TVRename.AppLogic.DownloadIdentifiers
{
    public class DownloadKodiMetaData : DownloadIdentifier
    {
        private static List<string> _doneNfo;

        public DownloadKodiMetaData() 
        {
            Reset();
        }

        public override DownloadType GetDownloadType() => DownloadType.DownloadMetaData;

        public override void NotifyComplete(FileInfo file)
        {
            if (file.FullName.EndsWith(".nfo", true, new CultureInfo("en")))
            {
                DownloadKodiMetaData._doneNfo.Add(file.FullName);
            }
            base.NotifyComplete(file);
        }

        public override ItemList ProcessShow(ProcessedSeries si, bool forceRefresh)
        {
            // for each tv show, optionally write a tvshow.nfo file
            if (ApplicationSettings.Instance.NFOShows)
            {
                ItemList actionList = new ItemList();
                FileInfo tvshownfo = FileHelper.FileInFolder(si.AutoAdd_FolderBase, "tvshow.nfo");

                bool needUpdate = !tvshownfo.Exists ||
                                  (si.TheSeries().Srv_LastUpdated > TimeZoneHelper.Epoch(tvshownfo.LastWriteTime)) ||
                                  (tvshownfo.LastWriteTime.ToUniversalTime().CompareTo(new DateTime(2009, 9, 13, 7, 30, 0, 0, DateTimeKind.Utc)) < 0);

                bool alreadyOnTheList = DownloadKodiMetaData._doneNfo.Contains(tvshownfo.FullName);

                if ((forceRefresh || needUpdate) && !alreadyOnTheList)
                {
                    actionList.Add(new ActionNFO(tvshownfo, si));
                    DownloadKodiMetaData._doneNfo.Add(tvshownfo.FullName);
                }
                return actionList;

            }

            return base.ProcessShow(si, forceRefresh);
        }

        public override ItemList ProcessEpisode(ProcessedEpisode dbep, FileInfo filo,bool forceRefresh)
        {
            if (ApplicationSettings.Instance.NFOEpisodes)
            {
                ItemList actionList = new ItemList();

                string fn = filo.RemoveExtension() + ".nfo";
                FileInfo nfo = FileHelper.FileInFolder(filo.Directory, fn);

                if (!nfo.Exists || (dbep.Srv_LastUpdated > TimeZoneHelper.Epoch(nfo.LastWriteTime)) || forceRefresh)
                {
                    //If we do not already have plans to put the file into place
                    if (!(DownloadKodiMetaData._doneNfo.Contains(nfo.FullName)))
                    {
                        actionList.Add(new ActionNFO(nfo, dbep));
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
