using System;
using System.Diagnostics;

namespace TVRename.AppLogic.Helpers
{
    public static class SystemHelper
    {
        public static bool StartProcess(string fileName, string arguments = null)
        {
            try
            {
                Process.Start(fileName, arguments);
                return true;
            }
            catch (Exception e)
            {
                // TODO: Put this back
                // logger.Error(e);
                return false;
            }
        }

    }
}
