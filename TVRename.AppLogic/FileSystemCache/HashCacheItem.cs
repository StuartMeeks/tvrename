namespace TVRename.AppLogic.FileSystemCache
{
    public class HashCacheItem
    {
        public long WhereInFile { get; }
        public long PieceSize { get; }
        public long FileSize { get; }
        public byte[] Hash { get; }

        public HashCacheItem(long whereInFile, long pieceSize, long fileSize, byte[] hash)
        {
            WhereInFile = whereInFile;
            PieceSize = pieceSize;
            FileSize = fileSize;
            Hash = hash;
        }
    }
}
