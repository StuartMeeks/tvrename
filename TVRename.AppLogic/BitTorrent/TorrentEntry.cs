namespace TVRename.AppLogic.BitTorrent
{
    /// <summary>
    /// Represents a torrent downloading in uTorrent
    /// </summary>
    public class TorrentEntry
    {
        public string DownloadingTo { get; }
        public int PercentComplete { get; }
        public string TorrentFile { get; }

        public TorrentEntry(string torrentfile, string downloadingTo, int percentComplete)
        {
            TorrentFile = torrentfile;
            DownloadingTo = downloadingTo;
            PercentComplete = percentComplete;
        }
    }
}
