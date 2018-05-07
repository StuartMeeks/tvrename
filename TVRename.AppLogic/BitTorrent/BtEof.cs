using System.IO;

namespace TVRename.AppLogic.BitTorrent
{
    public class BtEof : BtItemBase
    {
        public BtEof()
            : base(BtChunk.Eof)
        {
        }

        public override void Write(Stream sw)
        {
        }
    }

}
