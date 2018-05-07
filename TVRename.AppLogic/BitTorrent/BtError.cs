using System.IO;

namespace TVRename.AppLogic.BitTorrent
{
    public class BtError : BtItemBase
    {
        public string Message { get; set; }

        public BtError()
            : base(BtChunk.Error)
        {
            Message = string.Empty;
        }

        public override string AsText()
        {
            return $"Error: {Message}";
        }

        public override void Write(Stream sw)
        {
        }
    }
}
