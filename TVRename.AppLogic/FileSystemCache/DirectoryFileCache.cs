using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;

namespace TVRename.AppLogic.FileSystemCache
{
    public class DirectoryFileCache
    {
        private Dictionary<String, FileInfo[]> _fileCache;

        public DirectoryFileCache()
        {
            _fileCache = new Dictionary<string, FileInfo[]>();
        }

        public FileInfo[] LoadCacheFromFolder(string folder)
        {
            if (_fileCache.ContainsKey(folder))
            {
                return _fileCache[folder];
            }

            DirectoryInfo directory;
            try
            {
                directory = new DirectoryInfo(folder);
            }
            catch
            {
                _fileCache[folder] = null;
                return null;
            }

            if (!directory.Exists)
            {
                _fileCache[folder] = null;
                return null;
            }

            try
            {
                FileInfo[] files = directory.GetFiles();
                _fileCache[folder] = files;
                return files;
            }
            catch (System.IO.IOException)
            {
                return null;
            }
        }

        public void Clear()
        {
            _fileCache.Clear();
        }
    }
}
