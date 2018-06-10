using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using TVRename.AppLogic.ProcessedItems;
using TVRename.AppLogic.ScanItems.Items;
using TVRename.AppLogic.TheTvDb;

namespace TVRename.AppLogic.ScanItems.Actions
{
    public class DownloadImageAction : DownloadAction
    {
        private readonly ProcessedSeries ProcessedSeries;
        private readonly FileInfo DestinationFile;

        private readonly string _path;
        private readonly bool _mede8erShrink;

        public override string ActionName => "Download";
        public override string ActionProgressText => DestinationFile.Name;
        public override string ActionProduces => DestinationFile.FullName;
        public override long ActionSizeOfWork => 1000000;

        public override string ItemGroup => "lvgActionDownload";
        public override string ItemTargetFolder => DestinationFile == null ? null : DestinationFile.DirectoryName;
        public override int ItemIconNumber => 5;
        public override IgnoreItem ItemIgnore => DestinationFile == null ? null : new IgnoreItem(DestinationFile.FullName);

        public DownloadImageAction(ProcessedSeries processedSeries, ProcessedEpisode processedEpisode, FileInfo destinationFile, string path, bool mede8erShrink = false)
        {
            ProcessedSeries = processedSeries;
            ItemEpisode = processedEpisode;
            DestinationFile = destinationFile;

            _path = path;
            _mede8erShrink = mede8erShrink;
        }

        public static Image MaxSize(Image imgPhoto, int width, int height)
        {
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;

            float nPercentW = (width / (float)sourceWidth);
            float nPercentH = (height / (float)sourceHeight);

            int destWidth, destHeight;

            if (nPercentH < nPercentW)
            {
                destHeight = height;
                destWidth = (int)(sourceWidth * nPercentH);
            }
            else
            {
                destHeight = (int)(sourceHeight * nPercentW);
                destWidth = width;
            }

            Bitmap bmPhoto = new Bitmap(destWidth, destHeight, PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(Color.Black);
            grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(0, 0, destWidth, destHeight),
                new Rectangle(0, 0, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
        }

        public override bool PerformAction(ref bool pause, Statistics stats)
        {
            byte[] data = TheTvDbClient.Instance.GetTVDBDownload(_path);
            if (data == null || data.Length == 0)
            {
                ActionErrorText = "Unable to download " + _path;
                ActionError = true;
                ActionCompleted = true;
                return false;
            }

            if (_mede8erShrink)
            {
                // shrink images down to a maximum size of 156x232
                Image im = new Bitmap(new MemoryStream(data));
                if (im.Width > 156 || im.Height > 232)
                {
                    im = MaxSize(im, 156, 232);

                    using (MemoryStream m = new MemoryStream())
                    {
                        im.Save(m, ImageFormat.Jpeg);
                        data = m.ToArray();
                    }
                }
            }

            try
            {
                using (FileStream fs = new FileStream(DestinationFile.FullName, FileMode.Create))
                {
                    fs.Write(data, 0, data.Length);
                    fs.Close();
                }
            }
            catch (Exception e)
            {
                ActionErrorText = e.Message;
                ActionError = true;
                ActionCompleted = true;
                return false;
            }

            ActionCompleted = true;
            return true;
        }

        public override bool Equals(ItemBase other)
        {
            if (other is DownloadImageAction realOther)
            {
                return DestinationFile == realOther.DestinationFile;
            }

            return false;
        }

        public override int CompareTo(object other)
        {
            if (other == null)
            {
                return 1;
            }

            if (other.GetType() != typeof(DownloadImageAction))
            {
                return 1;
            }

            return CompareTo(other as DownloadImageAction);
        }

        public override int CompareTo(ItemBase other)
        {
            if (other is DownloadImageAction realOther)
            {
                return string.Compare(DestinationFile.FullName, realOther.DestinationFile.FullName, StringComparison.Ordinal);
            }

            return 0;
        }
    }
}
