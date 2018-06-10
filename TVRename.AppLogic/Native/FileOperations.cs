using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TVRename.AppLogic.Native
{
    public class FileOperations
    {
        private int _isCancelled;
        private int _filePercentCompleted;
        private string _source;
        private string _destination;

        private FileOperations()
        {
            _isCancelled = 0;
        }

        private event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        private void OnProgressChanged(double percent)
        {
            // only raise an event when progress has changed
            var handler = ProgressChanged;

            if ((int)percent > _filePercentCompleted)
            {
                _filePercentCompleted = (int)percent;

                handler?.Invoke(this, new ProgressChangedEventArgs(_filePercentCompleted, null));
            }
        }

        private event EventHandler Completed;
        private void OnCompleted()
        {
            var handler = Completed;
            handler?.Invoke(this, EventArgs.Empty);
        }

        public static void Copy(string source, string destination, bool overwrite, bool nobuffering)
        {
            new FileOperations().CopyInternal(source, destination, overwrite, nobuffering, null);
        }

        public static void Copy(string source, string destination, bool overwrite, bool nobuffering, EventHandler<ProgressChangedEventArgs> handler)
        {
            new FileOperations().CopyInternal(source, destination, overwrite, nobuffering, handler);
        }

        public static void Move(string source, string destination, bool overwrite)
        {
            new FileOperations().MoveInternal(source, destination, overwrite, null);
        }

        public static void Move(string source, string destination, bool overwrite, EventHandler<ProgressChangedEventArgs> handler)
        {
            new FileOperations().MoveInternal(source, destination, overwrite, handler);
        }

        private void CopyInternal(string source, string destination, bool overwrite, bool nobuffering, EventHandler<ProgressChangedEventArgs> handler)
        {
            try
            {
                var copyFileFlags = CopyFileFlags.COPY_FILE_RESTARTABLE;
                if (!overwrite)
                {
                    copyFileFlags |= CopyFileFlags.COPY_FILE_FAIL_IF_EXISTS;
                }

                if (nobuffering)
                {
                    copyFileFlags |= CopyFileFlags.COPY_FILE_NO_BUFFERING;
                }

                _source = source;
                _destination = destination;

                if (handler != null)
                {
                    ProgressChanged += handler;
                }

                var result = CopyFileEx(_source, _destination, CopyProgressHandler, IntPtr.Zero, ref _isCancelled, copyFileFlags);
                if (!result)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            catch (Exception)
            {
                if (handler != null)
                {
                    ProgressChanged -= handler;
                }

                throw;
            }
        }

        private void MoveInternal(string source, string destination, bool overwrite, EventHandler<ProgressChangedEventArgs> handler)
        {
            try
            {
                var moveFileFlags = MoveFileFlags.MOVE_FILE_COPY_ALLOWED;
                if (overwrite)
                {
                    moveFileFlags |= MoveFileFlags.MOVE_FILE_REPLACE_EXISTSING;
                }

                _source = source;
                _destination = destination;

                if (handler != null)
                {
                    ProgressChanged += handler;
                }

                var result = MoveFileWithProgress(_source, _destination, CopyProgressHandler, IntPtr.Zero, moveFileFlags);
                if (!result)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            catch (Exception)
            {
                if (handler != null)
                {
                    ProgressChanged -= handler;

                    throw;
                }
            }
        }

        #region PInvoke

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CopyFileEx(string lpExistingFileName, string lpNewFileName, CopyProgressRoutine lpProgressRoutine, IntPtr lpData, ref int pbCancel, CopyFileFlags dwCopyFlags);


        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool MoveFileWithProgress(string lpExistingFileName, string lpNewFileName, CopyProgressRoutine lpProgressRoutine, IntPtr lpData, MoveFileFlags dwCopyFlags);


        private delegate CopyProgressResult CopyProgressRoutine(long TotalFileSize, long TotalBytesTransferred, long StreamSize, long StreamBytesTransferred, uint dwStreamNumber, CopyProgressCallbackReason dwCallbackReason,
            IntPtr hSourceFile, IntPtr hDestinationFile, IntPtr lpData);

        internal enum CopyProgressResult : uint
        {
            PROGRESS_CONTINUE = 0,
            PROGRESS_CANCEL = 1,
            PROGRESS_STOP = 2,
            PROGRESS_QUIET = 3
        }

        internal enum CopyProgressCallbackReason : uint
        {
            CALLBACK_CHUNK_FINISHED = 0x00000000,
            CALLBACK_STREAM_SWITCH = 0x00000001
        }

        [Flags]
        internal enum CopyFileFlags : uint
        {
            COPY_FILE_FAIL_IF_EXISTS = 0x00000001,
            COPY_FILE_NO_BUFFERING = 0x00001000,
            COPY_FILE_RESTARTABLE = 0x00000002,
            COPY_FILE_OPEN_SOURCE_FOR_WRITE = 0x00000004,
            COPY_FILE_ALLOW_DECRYPTED_DESTINATION = 0x00000008
        }

        [Flags]
        internal enum MoveFileFlags : uint
        {
            MOVE_FILE_REPLACE_EXISTSING = 0x00000001,
            MOVE_FILE_COPY_ALLOWED = 0x00000002,
            MOVE_FILE_DELAY_UNTIL_REBOOT = 0x00000004,
            MOVE_FILE_WRITE_THROUGH = 0x00000008,
            MOVE_FILE_CREATE_HARDLINK = 0x00000010,
            MOVE_FILE_FAIL_IF_NOT_TRACKABLE = 0x00000020
        }

        private CopyProgressResult CopyProgressHandler(long total, long transferred, long streamSize, long streamByteTrans, uint dwStreamNumber,
            CopyProgressCallbackReason reason, IntPtr hSourceFile, IntPtr hDestinationFile, IntPtr lpData)
        {
            if (reason == CopyProgressCallbackReason.CALLBACK_CHUNK_FINISHED)
            {
                OnProgressChanged((transferred / (double)total) * 100.0);
            }

            if (transferred >= total)
            {
                OnCompleted();
            }

            return CopyProgressResult.PROGRESS_CONTINUE;
        }

        #endregion

    }
}
