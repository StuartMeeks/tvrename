using TVRename.AppLogic.ProcessedItems;

namespace TVRename.AppLogic
{
    public class PossibleDuplicateEpisode
    {
        public ProcessedEpisode EpisodeOne;
        public ProcessedEpisode EpisodeTwo;
        internal int SeasonNumber;
        public bool AirDatesMatch;
        public bool SimilarNames;
        public bool OneFound;
        public bool LargeFileSize;

        public ProcessedSeries ShowItem => EpisodeTwo.SI;
        public ProcessedEpisode Episode => EpisodeOne;

        public PossibleDuplicateEpisode(ProcessedEpisode episodeOne, ProcessedEpisode episodeTwo, int season,
            bool airDatesMatch, bool similarNames, bool oneFound, bool largeFileSize)
        {
            EpisodeTwo = episodeTwo;
            EpisodeOne = episodeOne;
            SeasonNumber = season;
            AirDatesMatch = airDatesMatch;
            SimilarNames = similarNames;
            OneFound = oneFound;
            LargeFileSize = largeFileSize;
        }
    }
}
