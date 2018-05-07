using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TVRename.AppLogic.BitTorrent
{
    public class BtDictionary : BtItemBase
    {
        public List<BtDictionaryItem> Items { get; }

        public BtDictionary()
            : base(BtChunk.Dictionary)
        {
            Items = new List<BtDictionaryItem>();
        }

        public override string AsText()
        {
            string r = "Dictionary=[";

            foreach (var btDictionaryItem in Items)
            {
                r += btDictionaryItem.AsText() + ',';
            }

            r = r.TrimEnd(',') + ']';

            return r;
        }

        public bool RemoveItem(string key)
        {
            if (Items.All(p => p.Key != key))
            {
                return false;
            }

            Items.RemoveAll(p => p.Key == key);
            return true;
        }

        public BtItemBase GetItem(string key, bool ignoreCase = false)
        {
            return Items.SingleOrDefault(p => string.Compare(p.Key, key,
                                                  ignoreCase
                                                      ? StringComparison.InvariantCultureIgnoreCase
                                                      : StringComparison.InvariantCulture) == 0);
        }

        public override void Write(Stream sw)
        {
            sw.WriteByte((byte)'d');
            foreach (BtDictionaryItem i in Items)
            {
                i.Write(sw);
            }

            sw.WriteByte((byte)'e');
        }
    }
}
