using System.IO;

namespace TVRename.AppLogic.BitTorrent
{
    public class BtListOrDictionaryEnd : BtItemBase
    {
        public BtListOrDictionaryEnd()
            : base(BtChunk.ListOrDictionaryEnd)
        {
        }

        public override void Write(Stream sw)
        {
            sw.WriteByte((byte)'e');
        }
    }

}
