// 
// Main website for TVRename is http://tvrename.com
// 
// Source code available at https://github.com/TV-Rename/tvrename
// 
// This code is released under GPLv3 https://github.com/TV-Rename/tvrename/blob/master/LICENSE.md
// 

using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using Newtonsoft.Json.Linq;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Security;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using NLog;

// Helpful functions and classes

namespace TVRename
{
    internal static partial class NativeMethods
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(UInt32 dwProcessId);
        private const UInt32 ATTACH_PARENT_PROCESS = 0xFFFFFFFF;

        // Attach to console window – this may modify the standard handles
        public static bool AttachParentConsole() =>AttachConsole(ATTACH_PARENT_PROCESS);


        public static void NewConsoleOutput(string text)
        {
            if (AllocConsole())
            {
                Console.Out.WriteLine(text);
                Console.In.ReadLine();

                FreeConsole();
            }
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeConsole();

    }



    public class FileSystemProperties
    {
        public  FileSystemProperties(long? totalBytes, long? freeBytes, long? availableBytes)
        {
            TotalBytes = totalBytes;
            FreeBytes = freeBytes;
            AvailableBytes = availableBytes;
        }

        /// <summary>
        /// Gets the total number of bytes on the drive.
        /// </summary>
        public long? TotalBytes { get; private set; }

        /// <summary>
        /// Gets the number of bytes free on the drive.
        /// </summary>
        public long? FreeBytes { get; private set; }

        /// <summary>
        /// Gets the number of bytes available on the drive (counts disk quotas).
        /// </summary>
        public long? AvailableBytes { get; private set; }
    }


    public static class FileHelper
    {
        public static int GetFilmLength(this FileInfo movieFile)
        {
            string duration;
            using (ShellObject shell = ShellObject.FromParsingName(movieFile.FullName))
            {
                // alternatively: shell.Properties.GetProperty("System.Media.Duration");
                IShellProperty prop = shell.Properties.System.Media.Duration;
                // Duration will be formatted as 00:44:08
                duration = prop.FormatForDisplay(PropertyDescriptionFormatOptions.None);
            }

            return 3600 * int.Parse(duration.Split(':')[0]) + 60 * int.Parse(duration.Split(':')[1]) +
                   int.Parse(duration.Split(':')[2]);

        }


        public static void GetFilmDetails(this FileInfo movieFile)
        {
            using (ShellPropertyCollection properties = new ShellPropertyCollection(movieFile.FullName))
            {
                foreach (IShellProperty prop in properties)
                {
                    string value = (prop.ValueAsObject == null)
                        ? ""
                        : prop.FormatForDisplay(PropertyDescriptionFormatOptions.None);
                    Console.WriteLine("{0} = {1}", prop.CanonicalName, value);
                }
            }
        }

   public static bool IsSubfolderOf(this string thisOne, string ofThat)
        {
            // need terminating slash, otherwise "c:\abc def" will match "c:\abc"
            thisOne += System.IO.Path.DirectorySeparatorChar.ToString();
            ofThat += System.IO.Path.DirectorySeparatorChar.ToString();
            int l = ofThat.Length;
            return ((thisOne.Length >= l) && (thisOne.Substring(0, l).ToLower() == ofThat.ToLower()));
        }



        public static string GBMB(this long value, int decimalPlaces = 2)
        {
            const long OneKb = 1024;
            const long OneMb = OneKb * 1024;
            const long OneGb = OneMb * 1024;
            const long OneTb = OneGb * 1024;

            double asTb = Math.Round((double)value / OneTb, decimalPlaces);
            double asGb = Math.Round((double)value / OneGb, decimalPlaces);
            double asMb = Math.Round((double)value / OneMb, decimalPlaces);
            double asKb = Math.Round((double)value / OneKb, decimalPlaces);
            double asB  = Math.Round((double)value, decimalPlaces);
            string chosenValue = asTb >= 1 ? $"{asTb:G3} TB"
                : asGb >= 1 ? $"{asGb:G3} GB"
                : asMb >= 1 ? $"{asMb:G3} MB"
                : asKb >= 1 ? $"{asKb:G3} KB"
                : $"{asB:G3} B";
            return chosenValue;
        }


        /// <summary>
        /// Gets the properties for this file system.
        /// </summary>
        /// <param name="volumeIdentifier">The path whose volume properties are to be queried.</param>
        /// <returns>A <see cref="FileSystemProperties"/> containing the properties for the specified file system.</returns>
        public static FileSystemProperties GetProperties(string volumeIdentifier)
        {
            if (NativeMethods.GetDiskFreeSpaceEx(volumeIdentifier, out ulong available, out ulong total, out ulong free))
            {
                return new FileSystemProperties((long)total, (long)free, (long)available);
            }
            return new FileSystemProperties(null, null, null);
        }

   
        public static void Rotate(string filenameBase)
        {
            if (File.Exists(filenameBase))
            {
                for (int i = 8; i >= 0; i--)
                {
                    string fn = filenameBase + "." + i;
                    if (File.Exists(fn))
                    {
                        string fn2 = filenameBase + "." + (i + 1);
                        if (File.Exists(fn2))
                            File.Delete(fn2);
                        File.Move(fn, fn2);
                    }
                }

                File.Copy(filenameBase, filenameBase + ".0");
            }
        }


        // see if showname is somewhere in filename
        internal static string TempPath(string v) => Path.GetTempPath() + v;

        public static string MakeValidPath(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            string directoryName = input;
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            foreach (char c in invalid)
            {
                directoryName = directoryName.Replace(c.ToString(), "");
            }

            return directoryName;

        }

    }






    public static class RegistryHelper {
        //From https://www.cyotek.com/blog/configuring-the-emulation-mode-of-an-internet-explorer-webbrowser-control THANKS
        //Needed to ensure webBrowser renders HTML 5 content

        private const string InternetExplorerRootKey = @"Software\Microsoft\Internet Explorer";
        private const string BrowserEmulationKey = InternetExplorerRootKey + @"\Main\FeatureControl\FEATURE_BROWSER_EMULATION";

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private enum BrowserEmulationVersion
        {
            Default = 0,
            Version7 = 7000,
            Version8 = 8000,
            Version8Standards = 8888,
            Version9 = 9000,
            Version9Standards = 9999,
            Version10 = 10000,
            Version10Standards = 10001,
            Version11 = 11000,
            Version11Edge = 11001
        }

        private static int GetInternetExplorerMajorVersion()
        {
            int result;

            result = 0;

            try
            {
                RegistryKey key;

                key = Registry.LocalMachine.OpenSubKey(InternetExplorerRootKey);

                if (key != null)
                {
                    object value;

                    value = key.GetValue("svcVersion", null) ?? key.GetValue("Version", null);

                    if (value != null)
                    {
                        string version;
                        int separator;

                        version = value.ToString();
                        separator = version.IndexOf('.');
                        if (separator != -1)
                        {
                            int.TryParse(version.Substring(0, separator), out result);
                        }
                    }
                }
            }
            catch (SecurityException se)
            {
                // The user does not have the permissions required to read from the registry key.
                logger.Error(se);
            }
            catch (UnauthorizedAccessException uae)
            {
                // The user does not have the necessary registry rights.
                logger.Error(uae);
            }

            return result;
        }
        
        private static BrowserEmulationVersion GetBrowserEmulationVersion()
        {
            BrowserEmulationVersion result;

            result = BrowserEmulationVersion.Default;

            try
            {
                RegistryKey key;

                key = Registry.CurrentUser.OpenSubKey(BrowserEmulationKey, true);
                if (key != null)
                {
                    string programName;
                    object value;

                    programName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                    value = key.GetValue(programName, null);

                    if (value != null)
                    {
                        result = (BrowserEmulationVersion)Convert.ToInt32(value);
                    }
                }
            }
            catch (SecurityException se)
            {
                // The user does not have the permissions required to read from the registry key.
                logger.Error(se);
            }
            catch (UnauthorizedAccessException uae)
            {
                // The user does not have the necessary registry rights.
                logger.Error(uae);
            }

            return result;
        }

        private static bool IsBrowserEmulationSet()
        {
            return GetBrowserEmulationVersion() != BrowserEmulationVersion.Default;
        }

        private static bool SetBrowserEmulationVersion(BrowserEmulationVersion browserEmulationVersion)
        {
            bool result;

            result = false;

            try
            {
                RegistryKey key;

                key = Registry.CurrentUser.OpenSubKey(BrowserEmulationKey, true);

                if (key != null)
                {
                    string programName;

                    programName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);

                    if (browserEmulationVersion != BrowserEmulationVersion.Default)
                    {
                        // if it's a valid value, update or create the value
                        key.SetValue(programName, (int)browserEmulationVersion, RegistryValueKind.DWord);
                        logger.Warn("SETTING REGISTRY:{0}-{1}-{2}-{3}",key.Name,programName, (int)browserEmulationVersion, RegistryValueKind.DWord.ToString());
                    }
                    else
                    {
                        // otherwise, remove the existing value
                        key.DeleteValue(programName, false);
                        logger.Warn("DELETING REGISTRY KEY:{0}-{1}", key.Name, programName);
                    }

                    result = true;
                }
            }
            catch (SecurityException se)
            {
                // The user does not have the permissions required to read from the registry key.
                logger.Error(se);
            }
            catch (UnauthorizedAccessException uae)
            {
                // The user does not have the necessary registry rights.
                logger.Error(uae);
            }

            return result;
        }

        private static bool SetBrowserEmulationVersion()
        {
            int ieVersion;
            BrowserEmulationVersion emulationCode;

            ieVersion = GetInternetExplorerMajorVersion();
            logger.Warn("IE Version {0} is identified",ieVersion );

            if (ieVersion >= 11)
            {
                emulationCode = BrowserEmulationVersion.Version11;
            }
            else
            {
                switch (ieVersion)
                {
                    case 10:
                        emulationCode = BrowserEmulationVersion.Version10;
                        break;
                    case 9:
                        emulationCode = BrowserEmulationVersion.Version9;
                        break;
                    case 8:
                        emulationCode = BrowserEmulationVersion.Version8;
                        break;
                    default:
                        emulationCode = BrowserEmulationVersion.Version7;
                        break;
                }
            }

            return SetBrowserEmulationVersion(emulationCode);
        }

        public static void UpdateBrowserEmulationVersion()
        {
            if (!IsBrowserEmulationSet())
            {
                logger.Warn("Updating the registry to ensure that the latest browser version is used");
                SetBrowserEmulationVersion();
            }
        }


    }

    public static class Helpers
    {

        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Gets a value indicating whether application is running under Mono.
        /// </summary>
        /// <value>
        ///   <c>true</c> if application is running under Mono; otherwise, <c>false</c>.
        /// </value>
        public static bool OnMono => Type.GetType("Mono.Runtime") != null;


        public static void Swap<T>(
            this IList<T> list,
            int firstIndex,
            int secondIndex
        )
        {
            Contract.Requires(list != null);
            Contract.Requires(firstIndex >= 0 && firstIndex < list.Count);
            Contract.Requires(secondIndex >= 0 && secondIndex < list.Count);
            if (firstIndex == secondIndex)
            {
                return;
            }
            T temp = list[firstIndex];
            list[firstIndex] = list[secondIndex];
            list[secondIndex] = temp;
        }
        
        public static void SafeInvoke(this Control uiElement, System.Action updater, bool forceSynchronous)
        {
            if (uiElement == null)
            {
                throw new ArgumentNullException("uiElement");
            }

            if (uiElement.InvokeRequired)
            {
                if (forceSynchronous)
                {
                    uiElement.Invoke((System.Action)delegate { SafeInvoke(uiElement, updater, forceSynchronous); });
                }
                else
                {
                    uiElement.BeginInvoke((System.Action)delegate { SafeInvoke(uiElement, updater, forceSynchronous); });
                }
            }
            else
            {
                if (uiElement.IsDisposed)
                {
                    throw new ObjectDisposedException("Control is already disposed.");
                }

                updater();
            }
        }

        /// <summary>
        /// Gets the application display version from the current assemblies <see cref="AssemblyInformationalVersionAttribute"/>.
        /// </summary>
        /// <value>
        /// The application display version.
        /// </value>
        public static string DisplayVersion
        {
            get
            {
                string v = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).Cast<AssemblyInformationalVersionAttribute>().First().InformationalVersion;

#if DEBUG
                v += " ** Debug Build **";
#endif

                return v;
            }
        }

        public static string pad(int i)
        {
            if (i.ToString().Length > 1)
            {
                return (i.ToString());
            }
            else
            {
                return ("0" + i);
            }
        }



        

        
        
        



        public static string GetCommonStartString(List<string> testValues)
        {
            string root = string.Empty;
            bool first = true;
            foreach (string test in testValues)
            {
                if (first)
                {
                    root = test;
                    first = false;
                }
                else
                {
                    root = GetCommonStartString(root, test);
                }
                
            }
            return root;
        }

        public static string TrimEnd(this string root, string ending)
        {
            if (!root.EndsWith(ending,StringComparison.OrdinalIgnoreCase)) return root;

            return root.Substring(0, root.Length - ending.Length);
        }

        public static string RemoveAfter(this string root, string ending)
        {
            if (root.IndexOf(ending, StringComparison.OrdinalIgnoreCase) !=-1)
                return   root.Substring(0, root.IndexOf(ending,StringComparison.OrdinalIgnoreCase));
            return root;
        }

        public static string TrimEnd(this string root, string[] endings)
        {
            string trimmedString = root;
            foreach (string ending in endings)
            {
                trimmedString = trimmedString.TrimEnd(ending);
            }

            return trimmedString;
        }

        public static string GetCommonStartString(string first, string second)
        {
            StringBuilder builder = new StringBuilder();
            
            int minLength = Math.Min(first.Length, second.Length);
            for (int i = 0; i < minLength; i++)
            {
                if (first[i].Equals(second[i]))
                {
                    builder.Append(first[i]);
                }
                else
                {
                    break;
                }
            }
            return builder.ToString();
        }

    }
}
