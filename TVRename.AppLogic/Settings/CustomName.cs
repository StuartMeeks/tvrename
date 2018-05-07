using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TVRename.AppLogic.Extensions;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.TheTvDb;

namespace TVRename.AppLogic.Settings
{
    public class CustomName
    {
        public string StyleString;

        public CustomName(CustomName other)
        {
            StyleString = other.StyleString;
        }

        public CustomName(string styleString)
        {
            StyleString = styleString;
        }

        public CustomName()
        {
            StyleString = DefaultStyle();
        }

        public static string DefaultStyle()
        {
            return Presets[1];
        }

        public static string OldNStyle(int n)
        {
            // enum class Style {Name_xxx_EpName = 0, Name_SxxEyy_EpName, xxx_EpName, SxxEyy_EpName, Eyy_EpName, 
            // Exx_Show_Sxx_EpName, yy_EpName, NameSxxEyy_EpName, xXxx_EpName };

            // for now, this maps onto the presets
            if (n >= 0 && n < 9)
            {
                return Presets[n];
            }

            return DefaultStyle();
        }

        public static readonly List<string> Presets = new List<string>
        {
            "{ShowName} - {Season}x{Episode}[-{Season}x{Episode2}] - {EpisodeName}",
            "{ShowName} - S{Season:2}E{Episode}[-E{Episode2}] - {EpisodeName}",
            "{ShowName} S{Season:2}E{Episode}[-E{Episode2}] - {EpisodeName}",
            "{Season}{Episode}[-{Season}{Episode2}] - {EpisodeName}",
            "{Season}x{Episode}[-{Season}x{Episode2}] - {EpisodeName}",
            "S{Season:2}E{Episode}[-E{Episode2}] - {EpisodeName}",
            "E{Episode}[-E{Episode2}] - {EpisodeName}",
            "{Episode}[-{Episode2}] - {ShowName} - 3 - {EpisodeName}",
            "{Episode}[-{Episode2}] - {EpisodeName}",
            "{ShowName} - S{Season:2}{AllEpisodes} - {EpisodeName}"
        };

        public string NameForExt(ProcessedEpisode processedEpisode, string extension = "", int folderNameLength = 0)
        {
            // set folderNameLength to have the filename truncated if the total path length is too long
            string r = NameForNoExt(processedEpisode, StyleString);

            int maxLenOK = 200 - (folderNameLength + (extension?.Length ?? 0));
            if (r.Length > maxLenOK)
            {
                r = r.Substring(0, maxLenOK);
            }

            if (!string.IsNullOrEmpty(extension))
            {
                if (!extension.StartsWith("."))
                {
                    r += ".";
                }

                r += extension;
            }

            return r;
        }

        public string GetTargetEpisodeName(TheTvDbEpisode episode, string showname, TimeZoneInfo tz, bool dvdOrder, bool urlEncode = false)
        {
            //note this is for an Episode and not a ProcessedEpisode
            string name = StyleString;
            string epname = episode.Name;
            name = name.ReplaceInsensitive("{ShowName}", showname);

            if (dvdOrder)
            {
                name = name.ReplaceInsensitive("{Season}", episode.DVDSeasonNumber.ToString());
                name = name.ReplaceInsensitive("{Season:2}", episode.DVDSeasonNumber.ToString("00"));
                name = name.ReplaceInsensitive("{Episode}", episode.DVDEpNum.ToString("00"));
                name = name.ReplaceInsensitive("{Episode2}", episode.DVDEpNum.ToString("00"));
                name = Regex.Replace(name, "{AllEpisodes}", episode.DVDEpNum.ToString("00"));

            }
            else
            {
                name = name.ReplaceInsensitive("{Season}", episode.AiredSeasonNumber.ToString());
                name = name.ReplaceInsensitive("{Season:2}", episode.AiredSeasonNumber.ToString("00"));
                name = name.ReplaceInsensitive("{Episode}", episode.AiredEpNum.ToString("00"));
                name = name.ReplaceInsensitive("{Episode2}", episode.AiredEpNum.ToString("00"));
                name = Regex.Replace(name, "{AllEpisodes}", episode.AiredEpNum.ToString("00"));

            }

            name = name.ReplaceInsensitive("{EpisodeName}", epname);
            name = name.ReplaceInsensitive("{Number}", "");
            name = name.ReplaceInsensitive("{Number:2}", "");
            name = name.ReplaceInsensitive("{Number:3}", "");

            DateTime? airdt = episode.GetAirDateDT(tz);

            if (airdt != null)
            {
                DateTime dt = (DateTime)airdt;
                name = name.ReplaceInsensitive("{ShortDate}", dt.ToString("d"));
                name = name.ReplaceInsensitive("{LongDate}", dt.ToString("D"));
                string ymd = dt.ToString("yyyy/MM/dd");
                if (urlEncode)
                {
                    ymd = System.Web.HttpUtility.UrlEncode(ymd);
                }
                name = name.ReplaceInsensitive("{YMDDate}", ymd);
            }
            else
            {
                name = name.ReplaceInsensitive("{ShortDate}", "---");
                name = name.ReplaceInsensitive("{LongDate}", "------");
                string ymd = "----/--/--";
                if (urlEncode)
                {
                    ymd = System.Web.HttpUtility.UrlEncode(ymd);
                }
                name = name.ReplaceInsensitive("{YMDDate}", ymd);
            }

            name = Regex.Replace(name, "([^\\\\])\\[.*?[^\\\\]\\]", "$1"); // remove optional parts
            name = name.Replace("\\[", "[");
            name = name.Replace("\\]", "]");

            return name.Trim();
        }

        public static readonly List<string> Tags = new List<string>
        {
            "{ShowName}",
            "{Season}",
            "{Season:2}",
            "{Episode}",
            "{Episode2}",
            "{EpisodeName}",
            "{Number}",
            "{Number:2}",
            "{Number:3}",
            "{ShortDate}",
            "{LongDate}",
            "{YMDDate}",
            "{AllEpisodes}"
        };

        public static string NameForNoExt(ProcessedEpisode processedEpisode, String styleString, bool urlEncode = false)
        {
            string name = styleString;
            string showname = processedEpisode.SI.ShowName;
            string epname = processedEpisode.Name;

            if (urlEncode)
            {
                showname = System.Web.HttpUtility.UrlEncode(showname);
                epname = System.Web.HttpUtility.UrlEncode(epname);
            }

            name = name.ReplaceInsensitive("{ShowName}", showname);
            name = name.ReplaceInsensitive("{Season}", processedEpisode.AppropriateSeasonNumber.ToString());
            name = name.ReplaceInsensitive("{Season:2}", processedEpisode.AppropriateSeasonNumber.ToString("00"));
            name = name.ReplaceInsensitive("{Episode}", processedEpisode.AppropriateEpNum.ToString("00"));
            name = name.ReplaceInsensitive("{Episode2}", processedEpisode.EpNum2.ToString("00"));
            name = name.ReplaceInsensitive("{EpisodeName}", epname);
            name = name.ReplaceInsensitive("{Number}", processedEpisode.OverallNumber.ToString());
            name = name.ReplaceInsensitive("{Number:2}", processedEpisode.OverallNumber.ToString("00"));
            name = name.ReplaceInsensitive("{Number:3}", processedEpisode.OverallNumber.ToString("000"));
            DateTime? airdt = processedEpisode.GetAirDateDT(false);

            if (airdt != null)
            {
                DateTime dt = (DateTime)airdt;
                name = name.ReplaceInsensitive("{ShortDate}", dt.ToString("d"));
                name = name.ReplaceInsensitive("{LongDate}", dt.ToString("D"));
                string ymd = dt.ToString("yyyy/MM/dd");
                if (urlEncode)
                {
                    ymd = System.Web.HttpUtility.UrlEncode(ymd);
                }
                name = name.ReplaceInsensitive("{YMDDate}", ymd);
            }
            else
            {
                name = name.ReplaceInsensitive("{ShortDate}", "---");
                name = name.ReplaceInsensitive("{LongDate}", "------");
                string ymd = "----/--/--";
                if (urlEncode)
                {
                    ymd = System.Web.HttpUtility.UrlEncode(ymd);
                }
                name = name.ReplaceInsensitive("{YMDDate}", ymd);
            }

            string allEps = "";
            for (int i = processedEpisode.AppropriateEpNum; i <= processedEpisode.EpNum2; i++)
            {
                allEps += "E" + i.ToString("00");
            }
            name = Regex.Replace(name, "{AllEpisodes}", allEps, RegexOptions.IgnoreCase);

            if (processedEpisode.EpNum2 == processedEpisode.AppropriateEpNum)
            {
                name = Regex.Replace(name, "([^\\\\])\\[.*?[^\\\\]\\]", "$1");
            }
            else
            {
                name = Regex.Replace(name, "([^\\\\])\\[(.*?[^\\\\])\\]", "$1$2");
            }

            name = name.Replace("\\[", "[");
            name = name.Replace("\\]", "]");

            return name.Trim();
        }
    }
}
