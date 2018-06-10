using System.IO;
using System.Text;

namespace TVRename.AppLogic.BitTorrent
{
    public class BtInteger : BtItemBase
    {
        public long Value { get; set; }

        public BtInteger()
            : base(BtChunk.Integer)
        {
            Value = 0;
        }

        public BtInteger(long value)
            : base(BtChunk.Integer)
        {
            Value = value;
        }

        public override string AsText()
        {
            return $"Integer={Value}";
        }

        public override void Write(Stream sw)
        {
            sw.WriteByte((byte)'i');
            var b = Encoding.ASCII.GetBytes(Value.ToString());
            sw.Write(b, 0, b.Length);
            sw.WriteByte((byte)'e');
        }
    }
}
