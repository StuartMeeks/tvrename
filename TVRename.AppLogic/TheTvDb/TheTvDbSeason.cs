using System;
using System.Collections.Generic;

namespace TVRename.AppLogic.TheTvDb
{
    public class TheTvDbSeason
    {
        public TheTvDbSeries Series;
        public List<TheTvDbEpisode> Episodes;

        public int SeasonId;
        public int SeasonNumber;

        public TheTvDbSeason(TheTvDbSeries series, int seasonNumber, int seasonId)
        {
            Series = series;
            Episodes = new List<TheTvDbEpisode>();

            SeasonNumber = seasonNumber;
            SeasonId = seasonId;
        }

        public TheTvDbSeasonStatus Status(TimeZoneInfo tz)
        {
            if (HasEpisodes)
            {
                if (HasAiredEpisodes(tz) && !HasUnairedEpisodes(tz))
                {
                    return TheTvDbSeasonStatus.Aired;
                }
                else if (HasAiredEpisodes(tz) && HasUnairedEpisodes(tz))
                {
                    return TheTvDbSeasonStatus.PartiallyAired;
                }
                else if (!HasAiredEpisodes(tz) && HasUnairedEpisodes(tz))
                {
                    return TheTvDbSeasonStatus.NoneAired;
                }
                else
                {
                    // Can happen if a Season has Episodes WITHOUT Airdates. 
                    //System.Diagnostics.Debug.Assert(false, string.Format("That is weird ... we have 'episodes' in '{0}' Season {1}, but none are aired, nor unaired. That case shouldn't actually occur !", this.TheSeries.Name,SeasonNumber));
                    return TheTvDbSeasonStatus.NoEpisodes;
                }
            }
            else
            {
                return TheTvDbSeasonStatus.NoEpisodes;
            }
        }

        private bool HasEpisodes => this.Episodes != null && this.Episodes.Count > 0;

        private bool HasUnairedEpisodes(TimeZoneInfo tz)
        {
            if (HasEpisodes)
            {
                foreach (TheTvDbEpisode e in this.Episodes)
                {
                    if (e.GetAirDateDT(tz).HasValue)
                    {
                        if (e.GetAirDateDT(tz).Value > System.DateTime.Now)
                            return true;
                    }
                }
            }
            return false;
        }

        private bool HasAiredEpisodes(TimeZoneInfo tz)
        {
            if (HasEpisodes)
            {
                foreach (TheTvDbEpisode e in this.Episodes)
                {
                    if (e.GetAirDateDT(tz).HasValue)
                    {
                        if (e.GetAirDateDT(tz).Value < System.DateTime.Now)
                            return true;
                    }
                }
            }
            return false;
        }

        public DateTime? LastAiredDate()
        {
            DateTime? returnValue = null;
            foreach (TheTvDbEpisode a in this.Episodes)
            {
                DateTime? episodeAirDate = a.FirstAired;

                //ignore episode if has no date
                if (!episodeAirDate.HasValue) continue;

                //ignore episode if it's in the future
                if (DateTime.Compare(episodeAirDate.Value.ToUniversalTime(), DateTime.UtcNow) > 0) continue;

                //If we don't have a best offer yet
                if (!returnValue.HasValue) returnValue = episodeAirDate.Value;
                //else the currently tested date is better than the current value
                else if (DateTime.Compare(episodeAirDate.Value, returnValue.Value) > 0) returnValue = episodeAirDate.Value;
            }
            return returnValue;

        }

        public string GetBannerPath()
        {
            return this.Series.GetSeasonBannerPath(this.SeasonNumber);
        }

        public string GetWideBannerPath()
        {
            return this.Series.GetSeasonWideBannerPath(this.SeasonNumber);
        }

        public void AddUpdateEpisode(TheTvDbEpisode newEpisode)
        {
            bool added = false;
            for (int i = 0; i < this.Episodes.Count; i++)
            {
                TheTvDbEpisode ep = this.Episodes[i];
                if (ep.EpisodeID == newEpisode.EpisodeID)
                {
                    this.Episodes[i] = newEpisode;
                    added = true;
                    break;
                }
            }
            if (!added)
                this.Episodes.Add(newEpisode);
        }

        public bool ContainsEpisode(int episodeNumber, bool dvdOrder)
        {
            foreach (TheTvDbEpisode ep in this.Episodes)
            {
                if (dvdOrder && ep.DVDEpNum == episodeNumber) return true;
                if (!dvdOrder && ep.AiredEpNum == episodeNumber) return true;
            }

            return false;
        }
    }
}
