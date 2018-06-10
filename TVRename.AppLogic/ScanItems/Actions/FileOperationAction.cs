using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVRename.AppLogic.Native;
using TVRename.AppLogic.Settings;

namespace TVRename.AppLogic.ScanItems.Actions
{
    public abstract class FileOperationAction : ActionBase
    {
        protected TidySettings Tidyup;

        protected void DeleteOrRecycleFile(FileInfo file)
        {
            if (file == null)
            {
                return;
            }

            if (Tidyup.DeleteEmptyIsRecycle)
            {
                RecycleBin.DeleteFile(file.FullName);
            }
            else
            {
                file.Delete();
            }
        }
        protected void DeleteOrRecycleFolder(DirectoryInfo directory)
        {
            if (directory == null)
            {
                return;
            }

            if (Tidyup.DeleteEmptyIsRecycle)
            {
                RecycleBin.DeleteFile(directory.FullName);
            }
            else
            {
                directory.Delete();
            }
        }
        protected void DoTidyup(DirectoryInfo directory)
        {
#if DEBUG
            Debug.Assert(Tidyup != null);
            Debug.Assert(Tidyup.DeleteEmpty);
#else
            if (this.Tidyup == null || !this.Tidyup.DeleteEmpty)
                return;
#endif
            // See if we should now delete the folder we just moved that file from.
            if (directory == null)
            {
                return;
            }

            //if there are sub-directories then we shouldn't remove this one
            var subDirectories = directory.GetDirectories();
            if (subDirectories
                .Select(subDirectory => Tidyup.EmptyIgnoreWordsArray.Any(word =>
                    subDirectory.Name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0))
                .Any(okToDelete => !okToDelete))
            {
                return;
            }
            //we know that each subfolder is OK to delete


            //if the directory is the root download folder do not delete
            if (ApplicationSettings.Instance.DownloadFoldersNames.Contains(directory.FullName))
            {
                return;
            }

            // Do not delete any monitor folders either
            if (ApplicationSettings.Instance.LibraryFoldersNames.Contains(directory.FullName))
            {
                return;
            }


            var files = directory.GetFiles();
            if (files.Length == 0)
            {
                // its empty, so just delete it
                DeleteOrRecycleFolder(directory);
                return;
            }


            if (Tidyup.EmptyIgnoreExtensions && !Tidyup.EmptyIgnoreWords)
            {
                return;
            }

            foreach (FileInfo fi in files)
            {
                bool okToDelete = Tidyup.EmptyIgnoreExtensions &&
                                  Array.FindIndex(Tidyup.EmptyIgnoreExtensionsArray, x => x == fi.Extension) != -1;

                if (okToDelete)
                {
                    continue;
                }

                // look in the filename
                if (Tidyup.EmptyIgnoreWordsArray.Any(word =>
                    fi.Name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    okToDelete = true;
                }

                if (!okToDelete)
                {
                    return;
                }
            }

            if (Tidyup.EmptyMaxSizeCheck)
            {
                // how many MB are we deleting?
                long totalBytes = files.Sum(fi => fi.Length);

                if (totalBytes / (1024 * 1024) > Tidyup.EmptyMaxSizeMb)
                {
                    return;
                }
            }

            DeleteOrRecycleFolder(directory);
        }
    }
}
