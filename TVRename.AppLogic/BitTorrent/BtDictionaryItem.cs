using System.IO;

namespace TVRename.AppLogic.BitTorrent
{
    public class BtDictionaryItem : BtItemBase
    {
        public BtItemBase Data { get; set; }
        public string Key { get; set; }

        public BtDictionaryItem()
            : base(BtChunk.DictionaryItem)
        {
        }

        public BtDictionaryItem(string key, BtItemBase data)
            : base(BtChunk.DictionaryItem)
        {
            Key = key;
            Data = data;
        }

        public override string AsText()
        {
            if (Key == "pieces" && Data.Type == BtChunk.String)
            {
                return "<File hash data>";
            }

            return string.Concat(Key, "=>", Data.AsText());
        }

        public override void Write(Stream sw)
        {
            new BtString(Key).Write(sw);
            Data.Write(sw);
        }
    }
}
