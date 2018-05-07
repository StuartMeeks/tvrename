using System.Timers;

namespace TVRename.AppLogic.TheTvDb
{
    public static class TheTvDbKeepAliveTimer
    {
        static Timer _timer;

        public static void Start()
        {
            _timer = new System.Timers.Timer(23 * 60 * 60 * 1000); // Set up the timer for 23 hours 
            _timer.Elapsed += _timer_Elapsed;
            _timer.Enabled = true; // Enable it
        }

        private static void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            TheTvDbClient.Instance.RefreshToken();
        }
    }
}
