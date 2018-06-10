using System;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.TheTvDb;

namespace TVRename.AppLogic
{
    public class ShowStatusColoringType
    {
        public bool IsMetaType { get; }
        public bool IsShowLevel { get; }
        public string Status { get; }

        public ShowStatusColoringType(string status, bool isMetaType, bool isShowLevel)
        {
            IsMetaType = isMetaType;
            IsShowLevel = isShowLevel;
            Status = status;
        }

        public string Text
        {
            get
            {
                if (IsShowLevel && IsMetaType)
                {
                    return $"Show Seasons Status: {StatusTextForDisplay}";
                }
                if (!IsShowLevel && IsMetaType)
                {
                    return $"Season Status: {StatusTextForDisplay}";
                }
                if (IsShowLevel && !IsMetaType)
                {
                    return $"Show Status: {StatusTextForDisplay}";
                }

                return string.Empty;
            }
        }

        private string StatusTextForDisplay
        {
            get
            {
                if (!IsMetaType)
                {
                    return Status;
                }
                if (IsShowLevel)
                {
                    var status = (ProcessedSeries.ShowAirStatus)Enum.Parse(typeof(ProcessedSeries.ShowAirStatus), Status);
                    switch (status)
                    {
                        case ProcessedSeries.ShowAirStatus.Aired:
                            return "All aired";
                        case ProcessedSeries.ShowAirStatus.NoEpisodesOrSeasons:
                            return "No Seasons or Episodes in Seasons";
                        case ProcessedSeries.ShowAirStatus.NoneAired:
                            return "None aired";
                        case ProcessedSeries.ShowAirStatus.PartiallyAired:
                            return "Partially aired";
                        default:
                            return Status;
                    }
                }
                else
                {
                    var status = (TheTvDbSeasonStatus)Enum.Parse(typeof(TheTvDbSeasonStatus), Status);
                    switch (status)
                    {
                        case TheTvDbSeasonStatus.Aired:
                            return "All aired";
                        case TheTvDbSeasonStatus.NoEpisodes:
                            return "No Episodes";
                        case TheTvDbSeasonStatus.NoneAired:
                            return "None aired";
                        case TheTvDbSeasonStatus.PartiallyAired:
                            return "Partially aired";
                        default:
                            return Status;
                    }
                }
            }
        }
    }
}
