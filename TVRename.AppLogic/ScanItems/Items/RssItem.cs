namespace TVRename.AppLogic.ScanItems.Items
{
    public class RssItem
    {
        public int Episode { get; set; }
        public int Season { get; set; }
        public string SeriesName { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }

        public RssItem(string url, string title, int season, int episode, string seriesName)
        {
            Url = url;
            Season = season;
            Episode = episode;
            Title = title;
            SeriesName = seriesName;
        }
    }
}
