namespace TVRename.AppLogic.Settings
{
    public class TidySettings
    {
        /// <summary>
        /// Delete empty folders after move
        /// </summary>
        public bool DeleteEmpty = false;

        /// <summary>
        /// Recycle rather than delete
        /// </summary>
        public bool DeleteEmptyIsRecycle = true;
        public bool EmptyIgnoreWords = false;
        public string EmptyIgnoreWordList = "sample";
        public bool EmptyIgnoreExtensions = false;
        public string EmptyIgnoreExtensionList = ".nzb;.nfo;.par2;.txt;.srt";
        public bool EmptyMaxSizeCheck = true;
        public int EmptyMaxSizeMB = 100;

        public string[] EmptyIgnoreExtensionsArray => EmptyIgnoreExtensionList.Split(';');
        public string[] EmptyIgnoreWordsArray => EmptyIgnoreWordList.Split(';');
    }
}
