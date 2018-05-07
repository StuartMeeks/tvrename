namespace TVRename.AppLogic.TheTvDb
{
    public enum TheTvDbSeasonStatus
    {
        /// <summary>
        /// Season completely aired ... no further shows in this season scheduled to date
        /// </summary>
        Aired,

        /// <summary>
        /// Season partially aired ... there are further shows in this season which are unaired to date
        /// </summary>
        PartiallyAired,

        /// <summary>
        /// Season completely unaired ... no show of this season as aired yet
        /// </summary>
        NoneAired,

        /// <summary>
        /// No episodes in season
        /// </summary>
        NoEpisodes,
    }
}
