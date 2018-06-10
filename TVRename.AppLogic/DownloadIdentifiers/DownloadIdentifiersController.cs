using System.Collections.Generic;
using System.IO;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems;

namespace TVRename.AppLogic.DownloadIdentifiers
{
    public class DownloadIdentifiersController
    {
        private readonly List<DownloadIdentifier> _identifiers;

        public DownloadIdentifiersController()
        {
            _identifiers = new List<DownloadIdentifier>
            {
                new DownloadEpisodeJpg(),
                new DownloadFanartJpg(),
                new DownloadFolderJpg(),
                new DownloadMede8erMetaData(),
                new DownloadpyTivoMetaData(),
                new DownloadSeriesJpg(),
                new DownloadKodiMetaData(),
                new DownloadKodiImages(),
                new IncorrectFileDates()
            };
        }

        public void NotifyComplete(FileInfo file)
        {
            foreach (var di in _identifiers)
            {
                di.NotifyComplete(file);
            }
        }

        public ItemList ProcessSeries(ProcessedSeries si)
        {
            var actionList = new ItemList();
            foreach (var di in _identifiers)
            {
                actionList.AddRange(di.ProcessSeries(si));
            }
            return actionList;
        }

        public ItemList ProcessSeason(ProcessedSeries si, string folder, int snum)
        {
            var actionList = new ItemList();
            foreach (var di in _identifiers)
            {
                actionList.AddRange(di.ProcessSeason(si, folder, snum));
            }
            return actionList;
        }

        public ItemList ProcessEpisode(ProcessedEpisode dbep, FileInfo filo)
        {
            var actionList = new ItemList();
            foreach (var di in _identifiers)
            {
                actionList.AddRange(di.ProcessEpisode(dbep, filo));
            }
            return actionList;
        }

        public void Reset()
        {
            foreach (var di in _identifiers)
            {
                di.Reset();
            }
        }

        public ItemList ForceUpdateShow(DownloadType dt, ProcessedSeries si)
        {
            var actionList = new ItemList();
            foreach (var di in _identifiers)
            {
                if (dt == di.GetDownloadType())
                {
                    actionList.AddRange(di.ProcessSeries(si, true));
                }
            }
            return actionList;
        }

        public ItemList ForceUpdateSeason(DownloadType dt, ProcessedSeries si, string folder, int snum)
        {
            var actionList = new ItemList();
            foreach (var di in _identifiers)
            {
                if (dt == di.GetDownloadType())
                {
                    actionList.AddRange(di.ProcessSeason(si, folder, snum, true));
                }
            }
            return actionList;
        }

        public ItemList ForceUpdateEpisode(DownloadType dt, ProcessedEpisode dbep, FileInfo filo)
        {
            var actionList = new ItemList();
            foreach (var di in _identifiers)
            {
                if (dt == di.GetDownloadType())
                {
                    actionList.AddRange(di.ProcessEpisode(dbep, filo, true));
                }
            }
            return actionList;
        }

    }
}
