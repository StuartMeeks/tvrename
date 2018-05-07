using System;
using System.Collections.Generic;
using System.Text;
using TVRename.AppLogic.TheTvDb;

namespace TVRename.AppLogic.ProcessedItems
{
    public class ProcessedEpisode : TheTvDbEpisode
    {
        public int EpNum2; // if we are a concatenation of episodes, this is the last one in the series. Otherwise, same as EpNum
        public bool Ignore;
        public bool NextToAir;
        public int OverallNumber;
        public ProcessedSeries SI;
        public ProcessedEpisodeType type;
        public List<TheTvDbEpisode> sourceEpisodes;

        public enum ProcessedEpisodeType { single, split, merged };


        public ProcessedEpisode(TheTvDbSeries ser, TheTvDbSeason airseas, TheTvDbSeason dvdseas, ProcessedSeries si)
            : base(ser, airseas, dvdseas)
        {
            this.NextToAir = false;
            this.OverallNumber = -1;
            this.Ignore = false;
            this.EpNum2 = si.DVDOrder ? this.DVDEpNum : this.AiredEpNum;
            this.SI = si;
            this.type = ProcessedEpisodeType.single;
        }

        public ProcessedEpisode(ProcessedEpisode O)
            : base(O)
        {
            this.NextToAir = O.NextToAir;
            this.EpNum2 = O.EpNum2;
            this.Ignore = O.Ignore;
            this.SI = O.SI;
            this.OverallNumber = O.OverallNumber;
            this.type = O.type;
        }

        public ProcessedEpisode(TheTvDbEpisode e, ProcessedSeries si)
            : base(e)
        {
            this.OverallNumber = -1;
            this.NextToAir = false;
            this.EpNum2 = si.DVDOrder ? this.DVDEpNum : this.AiredEpNum;
            this.Ignore = false;
            this.SI = si;
            this.type = ProcessedEpisodeType.single;
        }
        public ProcessedEpisode(TheTvDbEpisode e, ProcessedSeries si, ProcessedEpisodeType t)
            : base(e)
        {
            this.OverallNumber = -1;
            this.NextToAir = false;
            this.EpNum2 = si.DVDOrder ? this.DVDEpNum : this.AiredEpNum;
            this.Ignore = false;
            this.SI = si;
            this.type = t;
        }

        public ProcessedEpisode(TheTvDbEpisode e, ProcessedSeries si, List<TheTvDbEpisode> episodes)
            : base(e)
        {
            this.OverallNumber = -1;
            this.NextToAir = false;
            this.EpNum2 = si.DVDOrder ? this.DVDEpNum : this.AiredEpNum;
            this.Ignore = false;
            this.SI = si;
            this.sourceEpisodes = episodes;
            this.type = ProcessedEpisodeType.merged;
        }

        public int AppropriateSeasonNumber => this.SI.DVDOrder ? this.DVDSeasonNumber : this.AiredSeasonNumber;

        public TheTvDbSeason AppropriateSeason => this.SI.DVDOrder ? this.TheDVDSeason : this.TheAiredSeason;

        public int AppropriateEpNum
        {
            get => this.SI.DVDOrder ? DVDEpNum : this.AiredEpNum;
            set
            {
                if (this.SI.DVDOrder) DVDEpNum = value;
                else this.AiredEpNum = value;
            }
        }


        public string NumsAsString()
        {
            if (this.AppropriateEpNum == this.EpNum2)
                return this.AppropriateEpNum.ToString();
            else
                return this.AppropriateEpNum + "-" + this.EpNum2;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static int EPNumberSorter(ProcessedEpisode e1, ProcessedEpisode e2)
        {
            int ep1 = e1.AiredEpNum;
            int ep2 = e2.AiredEpNum;

            return ep1 - ep2;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static int DVDOrderSorter(ProcessedEpisode e1, ProcessedEpisode e2)
        {
            int ep1 = e1.AiredEpNum;
            int ep2 = e2.AiredEpNum;

            string n1 = e1.DVDEp;
            string n2 = e2.DVDEp;

            if ((!string.IsNullOrEmpty(n1)) && (!string.IsNullOrEmpty(n2)))
            {
                try
                {
                    int t1 = (int)(1000.0 * double.Parse(n1));
                    int t2 = (int)(1000.0 * double.Parse(n2));
                    ep1 = t1;
                    ep2 = t2;
                }
                catch (FormatException)
                {
                }
            }

            return ep1 - ep2;
        }

        public DateTime? GetAirDateDT(bool inLocalTime)
        {

            if (!inLocalTime)
                return GetAirDateDT();

            // do timezone adjustment
            return GetAirDateDT(this.SI.GetTimeZone());
        }

        public string HowLong()
        {
            DateTime? airsdt = GetAirDateDT(true);
            if (airsdt == null)
                return "";
            DateTime dt = (DateTime)airsdt;

            TimeSpan ts = dt.Subtract(DateTime.Now); // how long...
            if (ts.TotalHours < 0)
                return "Aired";
            else
            {
                int h = ts.Hours;
                if (ts.TotalHours >= 1)
                {
                    if (ts.Minutes >= 30)
                        h += 1;
                    return ts.Days + "d " + h + "h"; // +ts->Minutes+"m "+ts->Seconds+"s";
                }
                else
                    return Math.Round(ts.TotalMinutes) + "min";
            }
        }

        public string DayOfWeek()
        {
            DateTime? dt = GetAirDateDT(true);
            return (dt != null) ? dt.Value.ToString("ddd") : "-";
        }

        public string TimeOfDay()
        {
            DateTime? dt = GetAirDateDT(true);
            return (dt != null) ? dt.Value.ToString("t") : "-";
        }

    }
}
