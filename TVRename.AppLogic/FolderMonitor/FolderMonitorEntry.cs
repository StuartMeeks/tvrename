namespace TVRename.AppLogic.FolderMonitor
{
    public class FolderMonitorEntry
    {
        public int TvDbCode { get; set; }

        public string Folder { get; set; }
        public bool HasSeasonFoldersGuess { get; set; }
        public string SeasonFolderName { get; set; }

        public bool CodeKnown => !CodeUnknown;
        public bool CodeUnknown => TvDbCode == -1;

        public FolderMonitorEntry(string folder, bool seasonFolders, string seasonFolderName)
        {
            TvDbCode = -1;
            Folder = folder;
            HasSeasonFoldersGuess = seasonFolders;
            SeasonFolderName = seasonFolderName;
        }
    }
}
