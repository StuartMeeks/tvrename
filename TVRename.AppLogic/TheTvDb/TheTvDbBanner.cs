using System;
using System.Xml;
using Newtonsoft.Json.Linq;
using TVRename.AppLogic.Helpers;

namespace TVRename.AppLogic.TheTvDb
{
    public class TheTvDbBanner
    {
        public int BannerId;
        public int LanguageId;
        public string BannerPath;
        public string BannerType;
        public string Resolution;
        public double Rating;
        public int RatingCount;
        public int SeasonID;
        public int SeriesID;
        public string ThumbnailPath;

        public TheTvDbSeason Season;
        public TheTvDbSeries Series;

        // TODO: Put this back
        // private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public TheTvDbBanner(TheTvDbBanner other)
        {

            BannerId = other.BannerId;
            BannerPath = other.BannerPath;
            BannerType = other.BannerType;
            LanguageId = other.LanguageId;
            Resolution = other.Resolution;
            Rating = other.Rating;
            RatingCount = other.RatingCount;
            SeasonID = other.SeasonID;
            SeriesID = other.SeriesID;

            ThumbnailPath = other.ThumbnailPath;

            Season = other.Season;
            Series = other.Series;

        }

        public TheTvDbBanner(TheTvDbSeries series, TheTvDbSeason season)
        {
            SetDefaults(series, season);
        }

        public TheTvDbBanner(TheTvDbSeries series, TheTvDbSeason season, int? codeHint, XmlReader r) // TODO: Put this back , CommandLineArgs args)
        {
            try
            {
                SetDefaults(series, season);

                SeriesID = codeHint.Value;

                r.Read();
                if (r.Name != "Banner")
                    return;

                r.Read();
                while (!r.EOF)
                {
                    if ((r.Name == "Banner") && (!r.IsStartElement()))
                        break;

                    if (r.Name == "id")
                        BannerId = r.ReadElementContentAsInt();
                    else if (r.Name == "seriesid")
                        SeriesID = r.ReadElementContentAsInt(); // thetvdb series id
                    else if (r.Name == "seasonid")
                        SeasonID = r.ReadElementContentAsInt();
                    else if (r.Name == "BannerPath")
                        BannerPath = XmlHelper.ReadStringFixQuotesAndSpaces(r);
                    else if (r.Name == "BannerType")
                        BannerType = r.ReadElementContentAsString();
                    else if (r.Name == "LanguageId")
                        LanguageId = r.ReadElementContentAsInt();
                    else if (r.Name == "Resolution")
                        Resolution = r.ReadElementContentAsString();
                    else if (r.Name == "Rating")
                    {
                        string sn = r.ReadElementContentAsString();
                        double.TryParse(sn, out Rating);
                    }
                    else if (r.Name == "RatingCount")
                        RatingCount = r.ReadElementContentAsInt();
                    else if (r.Name == "Season")
                        SeasonID = r.ReadElementContentAsInt();
                    else if (r.Name == "ThumbnailPath") ThumbnailPath = r.ReadElementContentAsString();
                    else
                    {
                        if ((r.IsEmptyElement) || !r.IsStartElement())
                            r.ReadOuterXml();
                        else
                            r.Read();
                    }
                }
            }
            catch (XmlException e)
            {
                string message = "Error processing data from TheTVDB for a banner.";
                if (SeriesID != -1)
                    message += "\r\nSeries ID: " + SeriesID;
                if (BannerId != -1)
                    message += "\r\nBanner ID: " + BannerId;
                if (!string.IsNullOrEmpty(BannerPath))
                    message += "\r\nBanner Path: " + BannerPath;

                // TODO: Put this back
                // logger.Error(e, message);

                throw new TheTvDbException(e.Message);
            }
        }

        public TheTvDbBanner(int seriesId, JObject json, int LangId)
        {
            SetDefaults(null, null);
            // {
            //  "fileName": "string",
            //  "id": 0,
            //  "keyType": "string",
            //  "languageId": 0,
            //  "ratingsInfo": {
            //      "average": 0,
            //      "count": 0
            //      },
            //  "resolution": "string",
            //  "subKey": "string",         //May Contain Season Number
            //  "thumbnail": "string"
            //  }

            SeriesID = seriesId;

            BannerPath = (string)json["fileName"];
            BannerId = (int)json["id"];
            BannerType = (string)json["keyType"];
            LanguageId = (json["languageId"] == null) ? LangId : (int)json["languageId"];

            double.TryParse((string)(json["ratingsInfo"]["average"]), out Rating);
            RatingCount = (int)(json["ratingsInfo"]["count"]);

            Resolution = (string)json["resolution"];
            int.TryParse((string)json["subKey"], out SeasonID);
            ThumbnailPath = (string)json["thumbnail"];
        }


        public int SeasonNumber
        {
            get
            {
                if (Season != null)
                {
                    return Season.SeasonNumber;
                }

                return -1;
            }
        }

        public bool SameAs(TheTvDbBanner o)
        {
            return (BannerId == o.BannerId);
        }

        public bool IsSeriesPoster()
        {
            return ((BannerType == "poster"));
        }

        public bool IsSeriesBanner()
        {
            return ((BannerType == "series"));
        }

        public bool IsSeasonPoster()
        {
            return ((BannerType == "season"));
        }

        public bool IsSeasonBanner()
        {
            return ((BannerType == "seasonwide"));
        }

        public bool IsFanart()
        {
            return ((BannerType == "fanart"));
        }

        private void SetDefaults(TheTvDbSeries series, TheTvDbSeason season)
        {
            Season = season;
            Series = series;


            BannerId = -1;
            BannerPath = "";
            BannerType = "";
            LanguageId = -1;
            Resolution = "";
            Rating = -1;
            RatingCount = 0;
            SeasonID = -1;
            SeriesID = -1;

            ThumbnailPath = "";

        }

        public void SetSeriesSeason(TheTvDbSeries series, TheTvDbSeason season)
        {
            Series = series;
            Season = season;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Banner");

            XmlHelper.WriteElementToXML(writer, "id", BannerId);
            XmlHelper.WriteElementToXML(writer, "BannerPath", BannerPath);
            XmlHelper.WriteElementToXML(writer, "BannerType", BannerType);
            XmlHelper.WriteElementToXML(writer, "LanguageId", LanguageId);
            XmlHelper.WriteElementToXML(writer, "Resolution", Resolution);
            XmlHelper.WriteElementToXML(writer, "Rating", Rating);
            XmlHelper.WriteElementToXML(writer, "RatingCount", RatingCount);
            XmlHelper.WriteElementToXML(writer, "Season", SeasonID);
            XmlHelper.WriteElementToXML(writer, "ThumbnailPath", ThumbnailPath);

            writer.WriteEndElement(); //Banner
        }
    }
}
