using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using TVRename.AppLogic.Delegates;
using TVRename.AppLogic.FileSystemCache;

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

        public FileInfo FindLocalFileWithHashAt(byte[] findMe, long whereInFile, long pieceSize, long fileSize)
        {
            if (whereInFile < 0)
            {
                return null;
            }

            foreach (var dc in FileCache)
            {
                var fiTemp = dc.File;
                var flen = dc.Length;

                if (flen != fileSize || flen < whereInFile + pieceSize) // this file is wrong size || too small
                {
                    continue;
                }

                var theHash = CheckCache(fiTemp.FullName, whereInFile, pieceSize, fileSize);
                if (theHash == null)
                {
                    // not cached, figure it out ourselves
                    FileStream sr;
                    try
                    {
                        sr = new FileStream(fiTemp.FullName, FileMode.Open);
                    }
                    catch
                    {
                        return null;
                    }

                    var thePiece = new byte[pieceSize];
                    sr.Seek(whereInFile, SeekOrigin.Begin);
                    var n = sr.Read(thePiece, 0, (int)pieceSize);
                    sr.Close();

                    var sha1 = new SHA1Managed();

                    theHash = sha1.ComputeHash(thePiece, 0, n);
                    CacheThis(fiTemp.FullName, whereInFile, pieceSize, fileSize, theHash);
                }

                var allGood = true;
                for (var j = 0; j < 20; j++)
                {
                    if (theHash[j] == findMe[j])
                    {
                        continue;
                    }

                    allGood = false;
                    break;
                }

                if (allGood)
                {
                    return fiTemp;
                }
            } // while enum

            return null;
        }

        protected void CacheThis(string filename, long whereInFile, long piecesize, long fileSize, byte[] hash)
        {
            CacheItems++;

            if (!HashCache.ContainsKey(filename))
            {
                HashCache[filename] = new List<HashCacheItem>();
            }

            HashCache[filename].Add(new HashCacheItem(whereInFile, piecesize, fileSize, hash));
        }

        protected byte[] CheckCache(string filename, long whereInFile, long piecesize, long fileSize)
        {
            CacheChecks++;
            if (!HashCache.ContainsKey(filename))
            {
                return null;
            }

            foreach (var h in HashCache[filename])
            {
                if (h.WhereInFile != whereInFile || h.PieceSize != piecesize || h.FileSize != fileSize)
                {
                    continue;
                }

                CacheHits++;
                return h.Hash;
            }
            return null;
        }

        protected void BuildFileCache(string folder, bool subFolders)
        {
            if (FileCache != null && FileCacheIsFor != null && FileCacheIsFor == folder && FileCacheWithSubFolders == subFolders)
            {
                return;
            }

            FileCache = new DirectoryCache(null, folder, subFolders);
            FileCacheIsFor = folder;
            FileCacheWithSubFolders = subFolders;
        }

        public bool ProcessTorrentFile(string torrentFile)
        {
            // ----------------------------------------
            // read in torrent file

            // TODO: Put this back?
            //if (tvTree != null)
            //    tvTree.Nodes.Clear();

            var bel = new BtEncodeLoader();
            var btFile = bel.Load(torrentFile);
            var bti = btFile?.GetItem("info");

            if (bti == null || bti.Type != BtChunk.Dictionary)
            {
                return false;
            }

            var infoDict = (BtDictionary)bti;

            bti = infoDict.GetItem("piece length");
            if (bti == null || bti.Type != BtChunk.Integer)
            {
                return false;
            }

            var pieceSize = ((BtInteger)bti).Value;

            bti = infoDict.GetItem("pieces");
            if (bti == null || bti.Type != BtChunk.String)
            {
                return false;
            }

            var torrentPieces = (BtString)bti;

            bti = infoDict.GetItem("files");

            if (bti == null) // single file torrent
            {
                bti = infoDict.GetItem("name");
                if (bti == null || bti.Type != BtChunk.String)
                {
                    return false;
                }

                var di = (BtString)bti;
                var nameInTorrent = di.AsString();
                var fileSizeI = infoDict.GetItem("length");
                var fileSize = ((BtInteger)fileSizeI).Value;

                NewTorrentEntry(torrentFile, -1);
                if (DoHashChecking)
                {
                    var torrentPieceHash = torrentPieces.StringTwentyBytePiece(0);

                    var fi = FindLocalFileWithHashAt(torrentPieceHash, 0, pieceSize, fileSize);
                    if (fi != null)
                    {
                        FoundFileOnDiskForFileInTorrent(torrentFile, fi, -1, nameInTorrent);
                    }
                    else
                    {
                        DidNotFindFileOnDiskForFileInTorrent(torrentFile, -1, nameInTorrent);
                    }
                }
                FinishedTorrentEntry(torrentFile, -1, nameInTorrent);

                // don't worry about updating overallPosition as this is the only file in the torrent
            }
            else
            {
                long overallPosition = 0;
                long lastPieceLeftover = 0;

                if (bti.Type != BtChunk.List)
                {
                    return false;
                }

                var fileList = (BtList)bti;

                // list of dictionaries
                for (var i = 0; i < fileList.Items.Count; i++)
                {
                    Prog(100 * i / fileList.Items.Count);
                    if (fileList.Items[i].Type != BtChunk.Dictionary)
                    {
                        return false;
                    }

                    var file = (BtDictionary)fileList.Items[i];
                    var thePath = file.GetItem("path");
                    if (thePath.Type != BtChunk.List)
                    {
                        return false;
                    }

                    var pathList = (BtList)(thePath);
                    // want the last of the items in the list, which is the filename itself
                    var n = pathList.Items.Count - 1;
                    if (n < 0)
                    {
                        return false;
                    }

                    var fileName = (BtString)pathList.Items[n];
                    var fileSizeI = file.GetItem("length");
                    var fileSize = ((BtInteger)fileSizeI).Value;
                    var pieceNum = (int)(overallPosition / pieceSize);

                    if (overallPosition % pieceSize != 0)
                    {
                        pieceNum++;
                    }

                    NewTorrentEntry(torrentFile, i);

                    if (DoHashChecking)
                    {
                        var torrentPieceHash = torrentPieces.StringTwentyBytePiece(pieceNum);

                        var fi = FindLocalFileWithHashAt(torrentPieceHash, lastPieceLeftover, pieceSize, fileSize);
                        if (fi != null)
                        {
                            FoundFileOnDiskForFileInTorrent(torrentFile, fi, i, fileName.AsString());
                        }
                        else
                        {
                            DidNotFindFileOnDiskForFileInTorrent(torrentFile, i, fileName.AsString());
                        }
                    }

                    FinishedTorrentEntry(torrentFile, i, fileName.AsString());

                    var sizeInPieces = (int)(fileSize / pieceSize);
                    if (fileSize % pieceSize != 0)
                    {
                        sizeInPieces++; // another partial piece
                    }

                    lastPieceLeftover = (lastPieceLeftover + (int)(sizeInPieces * pieceSize - fileSize)) % pieceSize;
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

            Prog(0);

            return true;
        }
    }

}
