using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems.Items;
using TVRename.AppLogic.Settings;

namespace TVRename.AppLogic.ScanItems.Actions
{
    public class RssAction : ActionBase
    {
        // Local members
        private ProcessedEpisode ProcessedEpisode { get; }
        public RssItem RssItem { get; }
        public string TargetFile { get; }

        // Overrides for ItemBase
        public override string ItemTargetFolder => string.IsNullOrEmpty(TargetFile) ? null : new FileInfo(TargetFile).DirectoryName;
        public override string ItemGroup => "lvgActionDownloadRSS";
        public override int ItemIconNumber => 6;
        public override IgnoreItem ItemIgnore => string.IsNullOrEmpty(TargetFile) ? null : new IgnoreItem(TargetFile);

        // These overrides are for ActionItemBase
        public override string ActionName => "Get Torrent";
        public override string ActionProgressText => RssItem.Title;
        public override string ActionProduces => RssItem.Url;
        public override long ActionSizeOfWork => 1000000;


        // ctor
        public RssAction(RssItem rssItem, string targetFile, ProcessedEpisode processedEpisode)
        {
            ProcessedEpisode = processedEpisode;
            RssItem = rssItem;
            TargetFile = targetFile;
        }

        public override bool Equals(ItemBase other)
        {
            if (other is RssAction realOther)
            {
                return RssItem == realOther.RssItem;
            }

            return false;
        }

        public override int CompareTo(object other)
        {
            if (other == null)
            {
                return 1;
            }

            if (other.GetType() != typeof(RssAction))
            {
                return 1;
            }

            return CompareTo(other as RssAction);
        }

        public override int CompareTo(ItemBase other)
        {
            if (other is RssAction realOther)
            {
                return string.Compare(RssItem.Url, realOther.RssItem.Url, StringComparison.Ordinal);
            }

            return 1;
        }

        public override bool PerformAction(ref bool pause, Statistics stats)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = client.GetByteArrayAsync(RssItem.Url).Result;

                    if (response == null || response.Length == 0)
                    {
                        ActionError = true;
                        ActionErrorText = "No data downloaded";
                        ActionCompleted = true;

                        return false;
                    }

                    var saveTemp = Path.Combine(Path.GetTempPath(),
                        ApplicationSettings.Instance.FilenameFriendly(RssItem.Title));

                    if (new FileInfo(saveTemp).Extension.ToLower() != "torrent")
                    {
                        saveTemp += ".torrent";
                    }

                    File.WriteAllBytes(saveTemp, response);

                    Process.Start(ApplicationSettings.Instance.uTorrentPath,
                        "/directory \"" + (new FileInfo(TargetFile).Directory?.FullName) + "\" \"" + saveTemp + "\"");

                    ActionCompleted = true;

                    return true;
                }
            }
            catch (Exception e)
            {
                ActionError = true;
                ActionErrorText = e.Message;
                ActionCompleted = true;

                return false;
            }
        }
    }
}
