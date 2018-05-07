using System;
using System.Collections.Generic;
using System.IO;

using TVRename.AppLogic.Delegates;
using TVRename.AppLogic.FileSystemCache;

using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;

namespace TVRename.AppLogic.BitTorrent
{
    public abstract class BtCore
    {
        protected int CacheChecks;
        protected int CacheHits;
        protected int CacheItems;
        protected bool DoHashChecking;
        protected DirectoryCache FileCache;
        protected string FileCacheIsFor;
        protected bool FileCacheWithSubFolders;
        protected Dictionary<string, List<HashCacheItem>> HashCache;
        protected ProgressUpdatedDelegate ProgressDelegate;

        protected BtCore(ProgressUpdatedDelegate progressDelegate)
        {
            ProgressDelegate = progressDelegate;

            HashCache = new Dictionary<string, List<HashCacheItem>>();
            CacheChecks = CacheItems = CacheHits = 0;
            FileCache = null;
            FileCacheIsFor = null;
            FileCacheWithSubFolders = false;
        }

        protected void Prog(int percentComplete)
        {
            ProgressDelegate?.Invoke(percentComplete);
        }

        public abstract bool NewTorrentEntry(string torrentFile, int numberInTorrent);

        public abstract bool FoundFileOnDiskForFileInTorrent(string torrentFile, FileInfo onDisk, int numberInTorrent, string nameInTorrent);

        public abstract bool DidNotFindFileOnDiskForFileInTorrent(string torrentFile, int numberInTorrent, string nameInTorrent);

        public abstract bool FinishedTorrentEntry(string torrentFile, int numberInTorrent, string filename);

        public FileInfo FindLocalFileWithHashAt(byte[] findMe, Int64 whereInFile, Int64 pieceSize, Int64 fileSize)
        {
            if (whereInFile < 0)
                return null;

            foreach (DirectoryCacheItem dc in this.FileCache)
            //for (int i = 0; i < FileCache.Cache.Count; i++)
            {
                FileInfo fiTemp = dc.File;
                Int64 flen = dc.Length;

                if ((flen != fileSize) || (flen < (whereInFile + pieceSize))) // this file is wrong size || too small
                    continue;

                byte[] theHash = this.CheckCache(fiTemp.FullName, whereInFile, pieceSize, fileSize);
                if (theHash == null)
                {
                    // not cached, figure it out ourselves
                    FileStream sr = null;
                    try
                    {
                        sr = new FileStream(fiTemp.FullName, System.IO.FileMode.Open);
                    }
                    catch
                    {
                        return null;
                    }

                    byte[] thePiece = new byte[pieceSize];
                    sr.Seek(whereInFile, SeekOrigin.Begin);
                    int n = sr.Read(thePiece, 0, (int)pieceSize);
                    sr.Close();

                    System.Security.Cryptography.SHA1Managed sha1 = new System.Security.Cryptography.SHA1Managed();

                    theHash = sha1.ComputeHash(thePiece, 0, n);
                    this.CacheThis(fiTemp.FullName, whereInFile, pieceSize, fileSize, theHash);
                }

                bool allGood = true;
                for (int j = 0; j < 20; j++)
                {
                    if (theHash[j] != findMe[j])
                    {
                        allGood = false;
                        break;
                    }
                }
                if (allGood)
                    return fiTemp;
            } // while enum

            return null;
        }

        protected void CacheThis(string filename, Int64 whereInFile, Int64 piecesize, Int64 fileSize, byte[] hash)
        {
            this.CacheItems++;
            if (!this.HashCache.ContainsKey(filename))
                this.HashCache[filename] = new System.Collections.Generic.List<HashCacheItem>();
            this.HashCache[filename].Add(new HashCacheItem(whereInFile, piecesize, fileSize, hash));
        }

        protected byte[] CheckCache(string filename, Int64 whereInFile, Int64 piecesize, Int64 fileSize)
        {
            this.CacheChecks++;
            if (this.HashCache.ContainsKey(filename))
            {
                foreach (HashCacheItem h in this.HashCache[filename])
                {
                    if ((h.WhereInFile == whereInFile) && (h.PieceSize == piecesize) && (h.FileSize == fileSize))
                    {
                        this.CacheHits++;
                        return h.Hash;
                    }
                }
            }
            return null;
        }

        protected void BuildFileCache(string folder, bool subFolders)
        {
            if ((this.FileCache == null) || (this.FileCacheIsFor == null) || (this.FileCacheIsFor != folder) || (this.FileCacheWithSubFolders != subFolders))
            {
                this.FileCache = new DirectoryCache(null, folder, subFolders);
                this.FileCacheIsFor = folder;
                this.FileCacheWithSubFolders = subFolders;
            }
        }

        public bool ProcessTorrentFile(string torrentFile)
        {
            // ----------------------------------------
            // read in torrent file

            // TODO: Put this back?
            //if (tvTree != null)
            //    tvTree.Nodes.Clear();

            BtEncodeLoader bel = new BtEncodeLoader();
            BtFile btFile = bel.Load(torrentFile);

            if (btFile == null)
                return false;

            BtItemBase bti = btFile.GetItem("info");
            if ((bti == null) || (bti.Type != BtChunk.Dictionary))
                return false;

            BtDictionary infoDict = (BtDictionary)(bti);

            bti = infoDict.GetItem("piece length");
            if ((bti == null) || (bti.Type != BtChunk.Integer))
                return false;

            Int64 pieceSize = ((BtInteger)bti).Value;

            bti = infoDict.GetItem("pieces");
            if ((bti == null) || (bti.Type != BtChunk.String))
                return false;

            BtString torrentPieces = (BtString)(bti);

            bti = infoDict.GetItem("files");

            if (bti == null) // single file torrent
            {
                bti = infoDict.GetItem("name");
                if ((bti == null) || (bti.Type != BtChunk.String))
                    return false;

                BtString di = (BtString)(bti);
                string nameInTorrent = di.AsString();

                BtItemBase fileSizeI = infoDict.GetItem("length");
                Int64 fileSize = ((BtInteger)fileSizeI).Value;

                this.NewTorrentEntry(torrentFile, -1);
                if (this.DoHashChecking)
                {
                    byte[] torrentPieceHash = torrentPieces.StringTwentyBytePiece(0);

                    FileInfo fi = this.FindLocalFileWithHashAt(torrentPieceHash, 0, pieceSize, fileSize);
                    if (fi != null)
                        this.FoundFileOnDiskForFileInTorrent(torrentFile, fi, -1, nameInTorrent);
                    else
                        this.DidNotFindFileOnDiskForFileInTorrent(torrentFile, -1, nameInTorrent);
                }
                this.FinishedTorrentEntry(torrentFile, -1, nameInTorrent);

                // don't worry about updating overallPosition as this is the only file in the torrent
            }
            else
            {
                Int64 overallPosition = 0;
                Int64 lastPieceLeftover = 0;

                if (bti.Type != BtChunk.List)
                    return false;

                BtList fileList = (BtList)(bti);

                // list of dictionaries
                for (int i = 0; i < fileList.Items.Count; i++)
                {
                    this.Prog(100 * i / fileList.Items.Count);
                    if (fileList.Items[i].Type != BtChunk.Dictionary)
                        return false;

                    BtDictionary file = (BtDictionary)(fileList.Items[i]);
                    BtItemBase thePath = file.GetItem("path");
                    if (thePath.Type != BtChunk.List)
                        return false;
                    BtList pathList = (BtList)(thePath);
                    // want the last of the items in the list, which is the filename itself
                    int n = pathList.Items.Count - 1;
                    if (n < 0)
                        return false;
                    BtString fileName = (BtString)(pathList.Items[n]);

                    BtItemBase fileSizeI = file.GetItem("length");
                    Int64 fileSize = ((BtInteger)fileSizeI).Value;

                    int pieceNum = (int)(overallPosition / pieceSize);
                    if (overallPosition % pieceSize != 0)
                        pieceNum++;

                    this.NewTorrentEntry(torrentFile, i);

                    if (this.DoHashChecking)
                    {
                        byte[] torrentPieceHash = torrentPieces.StringTwentyBytePiece(pieceNum);

                        FileInfo fi = this.FindLocalFileWithHashAt(torrentPieceHash, lastPieceLeftover, pieceSize, fileSize);
                        if (fi != null)
                            this.FoundFileOnDiskForFileInTorrent(torrentFile, fi, i, fileName.AsString());
                        else
                            this.DidNotFindFileOnDiskForFileInTorrent(torrentFile, i, fileName.AsString());
                    }

                    this.FinishedTorrentEntry(torrentFile, i, fileName.AsString());

                    int sizeInPieces = (int)(fileSize / pieceSize);
                    if (fileSize % pieceSize != 0)
                        sizeInPieces++; // another partial piece

                    lastPieceLeftover = (lastPieceLeftover + (Int32)((sizeInPieces * pieceSize) - fileSize)) % pieceSize;
                    overallPosition += fileSize;
                } // for each file in the torrent
            }

            // TODO: Put this back?
            //if (tvTree != null)
            //{
            //    tvTree.BeginUpdate();
            //    btFile.Tree(tvTree.Nodes);
            //    tvTree.ExpandAll();
            //    tvTree.EndUpdate();
            //    tvTree.Update();
            //}

            this.Prog(0);

            return true;
        }
    }

}
