namespace TVRename.AppLogic
{
    /// <summary>
    /// Used for invalid (and general) character (and string) replacements in filenames
    /// </summary>
    public class Replacement
    {
        public string That { get; }
        public string This { get; }
        public bool IgnoreCase { get; }

        public Replacement(string @this, string that, bool ignoreCase)
        {
            if (that == null)
            {
                That = string.Empty;
            }
            This = @this;
            That = that;
            IgnoreCase = ignoreCase;
        }
    }
}
