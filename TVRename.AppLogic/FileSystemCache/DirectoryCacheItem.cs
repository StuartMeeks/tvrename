using System.IO;
using TVRename.AppLogic.Helpers;

namespace TVRename.AppLogic.FileSystemCache
{
    public class DirectoryCacheItem
    {
        public long Length { get; }
        public string SimplifiedFullName { get; }
        public FileInfo File { get; }

        public DirectoryCacheItem(FileInfo file)
        {
            File = file;
            SimplifiedFullName = FileHelper.SimplifyName(file.FullName);
            Length = file.Length;
        }
    }
}
