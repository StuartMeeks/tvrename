using System;

namespace TVRename.AppLogic.Extensions
{
    public static class DateTimeExtensions
    {
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static readonly DateTime WindowsStartDateTime = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixTime(this DateTime date)
        {
            return Convert.ToInt64((date.ToUniversalTime() - Epoch).TotalSeconds);
        }

        public static DateTime ToDateTime(this long unixTime)
        {
            return Epoch.AddSeconds(unixTime);
        }

    }
}
