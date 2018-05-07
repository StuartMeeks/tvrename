using System.Collections.Generic;
using TVRename.AppLogic.Helpers;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.Settings;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;

namespace TVRename.AppLogic.DownloadIdentifiers
{
    // ReSharper disable once InconsistentNaming
    public class DownloadMede8erMetaData : DownloadIdentifier
    {
        private List<string> _doneFiles;

        public DownloadMede8erMetaData()
        {
            Reset();
        }

        public override DownloadType GetDownloadType() => DownloadType.DownloadMetaData;

        public override ItemList ProcessShow(ProcessedSeries si, bool forceRefresh)
        {
            if (ApplicationSettings.Instance.Mede8erXML)
            {
                ItemList actionList = new ItemList();

                FileInfo tvshowxml = FileHelper.FileInFolder(si.AutoAdd_FolderBase, "series.xml");

                bool needUpdate = !tvshowxml.Exists || si.TheSeries().Srv_LastUpdated > TimeZoneHelper.Epoch(tvshowxml.LastWriteTime);

                if ((forceRefresh || needUpdate) && _doneFiles.Contains(tvshowxml.FullName))
                {
                    _doneFiles.Add(tvshowxml.FullName);
                    actionList.Add(new ActionMede8erXML(tvshowxml, si));
                }

                //Updates requested by zakwaan@gmail.com on 18/4/2013
                FileInfo viewxml = FileHelper.FileInFolder(si.AutoAdd_FolderBase, "View.xml");
                if ((!viewxml.Exists) && (!_doneFiles.Contains(viewxml.FullName)))
                {
                    _doneFiles.Add(viewxml.FullName);
                    actionList.Add(new ActionMede8erViewXML(viewxml, si));
                }


                return actionList;
            }

            return base.ProcessShow(si, forceRefresh);
        }

        public override ItemList ProcessSeason(ProcessedSeries si, string folder, int snum, bool forceRefresh)
        {
            if (ApplicationSettings.Instance.Mede8erXML)
            {
                ItemList actionList = new ItemList();

                //Updates requested by zakwaan@gmail.com on 18/4/2013
                FileInfo viewxml = FileHelper.FileInFolder(folder, "View.xml");
                if (!viewxml.Exists) actionList.Add(new ActionMede8erViewXML(viewxml, si, snum));


                return actionList;
            }

            return base.ProcessSeason(si, folder, snum, forceRefresh);
        }

        public override ItemList ProcessEpisode(ProcessedEpisode dbep, FileInfo filo, bool forceRefresh)
        {
            if (ApplicationSettings.Instance.Mede8erXML)
            {
                ItemList actionList = new ItemList();
                string fn = filo.RemoveExtension() + ".xml";
                FileInfo nfo = FileHelper.FileInFolder(filo.Directory, fn);

                if (forceRefresh || !nfo.Exists || (dbep.Srv_LastUpdated > TimeZoneHelper.Epoch(nfo.LastWriteTime)))
                    actionList.Add(new ActionMede8erXML(nfo, dbep));

                return actionList;

            }
            return base.ProcessEpisode(dbep, filo, forceRefresh);
        }

        public sealed override void Reset()
        {
            _doneFiles = new List<string>();
        }

    }
}
