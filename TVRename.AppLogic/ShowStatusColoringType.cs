using System;

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
                if (!this.IsMetaType)
                {
                    return this.Status;
                }
                if (this.IsShowLevel)
                {
                    ShowItem.ShowAirStatus status = (ShowItem.ShowAirStatus)Enum.Parse(typeof(ShowItem.ShowAirStatus), this.Status);
                    switch (status)
                    {
                        case ShowItem.ShowAirStatus.Aired:
                            return "All aired";
                        case ShowItem.ShowAirStatus.NoEpisodesOrSeasons:
                            return "No Seasons or Episodes in Seasons";
                        case ShowItem.ShowAirStatus.NoneAired:
                            return "None aired";
                        case ShowItem.ShowAirStatus.PartiallyAired:
                            return "Partially aired";
                        default:
                            return this.Status;
                    }
                }
                else
                {
                    Season.SeasonStatus status = (Season.SeasonStatus)Enum.Parse(typeof(Season.SeasonStatus), this.Status);
                    switch (status)
                    {
                        case Season.SeasonStatus.Aired:
                            return "All aired";
                        case Season.SeasonStatus.NoEpisodes:
                            return "No Episodes";
                        case Season.SeasonStatus.NoneAired:
                            return "None aired";
                        case Season.SeasonStatus.PartiallyAired:
                            return "Partially aired";
                        default:
                            return this.Status;
                    }
                }
            }
        }
    }
}
