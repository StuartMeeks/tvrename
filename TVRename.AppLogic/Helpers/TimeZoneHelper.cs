using System;
using System.Linq;

namespace TVRename.AppLogic.Helpers
{
    public static class TimeZoneHelper
    {
        public static string[] ZoneNames()
        {
            return TimeZoneInfo.GetSystemTimeZones().Select(p => p.DisplayName).ToArray();
        }

        public static TimeZoneInfo TimeZoneFor(string name)
        {
            return TimeZoneInfo.GetSystemTimeZones().SingleOrDefault(p => p.DisplayName == name);
        }

        public static string DefaultTimeZone()
        {
            return "Eastern Standard Time";
        }

        public static string TimeZoneForNetwork(string network)
        {
            string[] UKTV = { "Sky Atlantic (UK)", "BBC One", "Sky1", "BBC Two", "ITV", "Nick Jr.", "BBC Three", "Channel 4", "CBeebies", "Sky Box Office", "Watch", "ITV2", "National Geographic (UK)", "V", "ITV Encore", "ITV1", "BBC", "E4", "Channel 5 (UK)", "BBC Four", "ITVBe" };
            string[] AusTV = { "ABC4Kids", "Stan", "Showcase (AU)", "PBS Kids Sprout", "SBS (AU)", "Nine Network", "ABC1", "ABC (AU)" };
            if (string.IsNullOrWhiteSpace(network)) return DefaultTimeZone();
            if (UKTV.Contains(network)) return "GMT Standard Time";
            if (AusTV.Contains(network)) return "AUS Eastern Standard Time";

            return DefaultTimeZone();
        }

        public static DateTime AdjustRemoteTimeToLocalTime(DateTime remoteTime, TimeZoneInfo remoteTimeZone)
        {
            return remoteTimeZone == null
                ? remoteTime
                : TimeZoneInfo.ConvertTime(remoteTime, remoteTimeZone, TimeZoneInfo.Local);
        }

        /// <summary>
        /// Unix epoch time for now (seconds since midnight 1 jan 1970 UTC)
        /// </summary>
        /// <returns>long value containing the epoch for now</returns>
        public static long Epoch()
        {
            return Epoch(DateTime.UtcNow);
        }

        public static long Epoch(DateTime dateTime)
        {
            return (long)dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
        }
    }
}
