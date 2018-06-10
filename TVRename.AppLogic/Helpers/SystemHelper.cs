using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

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
            catch (Exception)
            {
                // TODO: Put this back
                // logger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Gets the application display version from the current assemblies <see cref="AssemblyInformationalVersionAttribute"/>.
        /// </summary>
        /// <value>
        /// The application display version.
        /// </value>
        public static string GetDisplayVersion
        {
            get
            {
                var v = Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                    .Cast<AssemblyInformationalVersionAttribute>().First().InformationalVersion;
#if DEBUG
                v += " ** Debug Build **";
#endif
                return v;
            }
        }

    }
}
