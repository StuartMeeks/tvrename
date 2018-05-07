using System.Collections.Generic;
using System.IO;

namespace TVRename.AppLogic.BitTorrent
{
    public class BtList : BtItemBase
    {
        public List<BtItemBase> Items { get; }

        public BtList()
            : base(BtChunk.List)
        {
            Items = new List<BtItemBase>();
        }

        public override string AsText()
        {
            string r = "List={";

            foreach (var btItemBase in Items)
            {
                r += btItemBase.AsText() + ',';
            }

            r = r.TrimEnd(',') + '}';

            return r;
        }

        public override void Write(Stream sw)
        {
            sw.WriteByte((byte)'l');
            foreach (BtItemBase i in this.Items)
            {
                i.Write(sw);
            }

            sw.WriteByte((byte)'e');
        }
    }
}
