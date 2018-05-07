using System;
using TVRename.AppLogic.ProcessedItems;

namespace TVRename.AppLogic.ScanItems
{
    class RssActionItem
    {
        public RSSItem RSS;
        public string TheFileNoExt;

        public ActionRSS(RSSItem rss, string toWhereNoExt, ProcessedEpisode processedEpisode)
        {
            this.Episode = pe;
            this.RSS = rss;
            this.TheFileNoExt = toWhereNoExt;
        }

        #region Action Members

        public override string ProgressText => this.RSS.Title;


        public override string Name => "Get Torrent";

        public override long SizeOfWork => 1000000;

        public override string Produces => this.RSS.URL;

        public override bool Go(ref bool pause, TVRenameStats stats)
        {
            System.Net.WebClient wc = new System.Net.WebClient();
            try
            {
                byte[] r = wc.DownloadData(this.RSS.URL);
                if ((r == null) || (r.Length == 0))
                {
                    this.Error = true;
                    this.ErrorText = "No data downloaded";
                    this.Done = true;
                    return false;
                }

                string saveTemp = Path.GetTempPath() + System.IO.Path.DirectorySeparatorChar + TVSettings.Instance.FilenameFriendly(this.RSS.Title);
                if (new FileInfo(saveTemp).Extension.ToLower() != "torrent")
                    saveTemp += ".torrent";
                File.WriteAllBytes(saveTemp, r);

                System.Diagnostics.Process.Start(TVSettings.Instance.uTorrentPath, "/directory \"" + (new FileInfo(this.TheFileNoExt).Directory.FullName) + "\" \"" + saveTemp + "\"");

                this.Done = true;
                return true;
            }
            catch (Exception e)
            {
                this.ErrorText = e.Message;
                this.Error = true;
                this.Done = true;
                return false;
            }
        }

        #endregion

        #region Item Members

        public override bool SameAs(Item o)
        {
            return (o is ActionRSS) && ((o as ActionRSS).RSS == this.RSS);
        }

        public override int Compare(Item o)
        {
            ActionRSS rss = o as ActionRSS;
            return rss == null ? 0 : this.RSS.URL.CompareTo(rss.RSS.URL);
        }

        #endregion

        #region Item Members

        public override IgnoreItem Ignore
        {
            get
            {
                if (string.IsNullOrEmpty(this.TheFileNoExt))
                    return null;
                return new IgnoreItem(this.TheFileNoExt);
            }
        }

        public override string TargetFolder
        {
            get
            {
                if (string.IsNullOrEmpty(this.TheFileNoExt))
                    return null;
                return new FileInfo(this.TheFileNoExt).DirectoryName;
            }
        }

        public override string ScanListViewGroup => "lvgActionDownloadRSS";

        public override int IconNumber => 6;

        #endregion
    }
}
