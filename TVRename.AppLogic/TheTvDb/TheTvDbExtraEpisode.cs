namespace TVRename.AppLogic.TheTvDb
{
    public class TheTvDbExtraEpisode
    {
        public int SeriesId { get; }
        public int EpisodeId { get; }
        public bool Done { get; set; }

        public TheTvDbExtraEpisode(int seriesId, int episodeId)
        {
            SeriesId = seriesId;
            EpisodeId = episodeId;
            Done = false;
        }
    }
}
