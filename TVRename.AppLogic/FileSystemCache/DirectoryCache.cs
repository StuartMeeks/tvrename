using System;
using System.Collections.Generic;

using Alphaleonis.Win32.Filesystem;
using NLog;
using TVRename.AppLogic.Delegates;

namespace TVRename.AppLogic.FileSystemCache
{
    public class DirectoryCache : List<DirectoryCacheItem>
    {
        public Logger Logger { get; set; }

        private ProgressUpdatedDelegate _progressUpdatedDelegate;

        public DirectoryCache()
        {
        }

        public DirectoryCache(ProgressUpdatedDelegate progressUpdatedDelegate, string folder, bool includeSubFolders)
        {
            _progressUpdatedDelegate = progressUpdatedDelegate;

            BuildDirectoryCache(progressUpdatedDelegate, 0, 0, folder, includeSubFolders);
        }

        protected virtual void OnProgressUpdated(int count, int totalFiles)
        {
            if (_progressUpdatedDelegate != null && totalFiles != 0)
            {
                _progressUpdatedDelegate.Invoke(100 * (decimal)count / totalFiles);
            }
        }

        public int AddFolder(ProgressUpdatedDelegate progressUpdatedDelegate, int initialCount, int totalFiles, string folder, bool includeSubFolders)
        {
            return BuildDirectoryCache(progressUpdatedDelegate, initialCount, totalFiles, folder, includeSubFolders);
        }

        private int BuildDirectoryCache(ProgressUpdatedDelegate progressUpdatedDelegate, int count, int totalFiles, string folder, bool includeSubFolders)
        {
            if (!Directory.Exists(folder))
            {
                Logger.Error($"The search folder \"{folder}\" does not exist.\n");
                return count;
            }

            try
            {
                var directory = new DirectoryInfo(folder);
                if (!directory.Exists)
                {
                    return count;
                }

                var files = directory.GetFiles();
                foreach (FileInfo file in files)
                {
                    count++;

                    Add(new DirectoryCacheItem(file));
                    OnProgressUpdated(count, totalFiles);

                    if (includeSubFolders)
                    {
                        var subDirectories = directory.GetDirectories();
                        foreach (var subDirectory in subDirectories)
                        {
                            count = BuildDirectoryCache(progressUpdatedDelegate, count, totalFiles, subDirectory.FullName, includeSubFolders);
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException unauthorizedAccessException)
            {
                Logger.Info(unauthorizedAccessException);
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
            }

            return count;
        }
        public static int CountFiles(string folder, bool includeSubFolders)
        {
            int fileCount = 0;
            if (!Directory.Exists(folder))
            {
                return fileCount;
            }

            try
            {
                DirectoryInfo directory = new DirectoryInfo(folder);
                fileCount = directory.GetFiles().Length;

                if (includeSubFolders)
                {
                    DirectoryInfo[] subDirectories = directory.GetDirectories();
                    foreach (DirectoryInfo subDirectory in subDirectories)
                    {
                        fileCount += CountFiles(subDirectory.FullName, includeSubFolders);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }

            return fileCount;
        }
    }
}
