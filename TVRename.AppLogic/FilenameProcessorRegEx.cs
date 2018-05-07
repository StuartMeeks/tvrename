namespace TVRename.AppLogic
{
    /// <summary>
    /// A regular expression to find the season and episode number in a filename
    /// </summary>
    public class FilenameProcessorRegEx
    {
        public string RegEx { get; }
        public bool UseFullPath { get; }
        public bool Enabled { get; }
        public string Notes { get; }

        public FilenameProcessorRegEx(string regEx, bool useFullPath, bool enabled, string notes)
        {
            RegEx = regEx;
            UseFullPath = useFullPath;
            Enabled = enabled;
            Notes = notes;
        }
    }
}
