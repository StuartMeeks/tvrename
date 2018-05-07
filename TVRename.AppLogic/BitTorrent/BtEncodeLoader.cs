using System;
using System.IO;
using NLog;

namespace TVRename.AppLogic.BitTorrent
{
    public class BtEncodeLoader
    {
        public Logger Logger { get; set; }

        public BtEncodeLoader()
        {

        }

        public BtItemBase ReadString(Stream sr, long length)
        {
            BinaryReader br = new BinaryReader(sr);
            byte[] c = br.ReadBytes((int)length);
            BtString bts = new BtString
            {
                Data = c
            };

            return bts;
        }

        public BtItemBase ReadInt(FileStream sr)
        {
            long r = 0;
            int c;
            bool neg = false;
            while ((c = sr.ReadByte()) != 'e')
            {
                if (c == '-')
                {
                    neg = true;
                }
                else if ((c >= '0') && (c <= '9'))
                {
                    r = (r * 10) + c - '0';
                }
            }

            if (neg)
            {
                r = -r;
            }

            BtInteger bti = new BtInteger
            {
                Value = r
            };
            return bti;
        }

        public BtItemBase ReadDictionary(FileStream sr)
        {
            BtDictionary d = new BtDictionary();
            for (; ; )
            {
                BtItemBase next = this.ReadNext(sr);
                if (next.Type == BtChunk.ListOrDictionaryEnd || next.Type == BtChunk.Eof)
                {
                    return d;
                }

                if (next.Type != BtChunk.String)
                {
                    BtError e = new BtError
                    {
                        Message = "Didn't get string as first of pair in dictionary"
                    };
                    return e;
                }

                BtDictionaryItem di = new BtDictionaryItem
                {
                    Key = ((BtString)next).AsString(),
                    Data = this.ReadNext(sr)
                };

                d.Items.Add(di);
            }
        }

        public BtItemBase ReadList(FileStream sr)
        {
            BtList ll = new BtList();
            for (; ; )
            {
                BtItemBase next = ReadNext(sr);
                if (next.Type == BtChunk.ListOrDictionaryEnd)
                {
                    return ll;
                }

                ll.Items.Add(next);
            }
        }

        public BtItemBase ReadNext(FileStream sr)
        {
            if (sr.Length == sr.Position)
            {
                return new BtEof();
            }

            // Read the next character from the stream to see what is next

            int c = sr.ReadByte();
            if (c == 'd')
            {
                return ReadDictionary(sr);
            }
            if (c == 'l')
            {
                return ReadList(sr); // list
            }
            if (c == 'i')
            {
                return ReadInt(sr); // integer
            }
            if (c == 'e')
            {
                return new BtListOrDictionaryEnd(); // end of list/dictionary/etc.
            }
            if ((c >= '0') && (c <= '9')) // digits mean it is a string of the specified length
            {
                string r = Convert.ToString(c - '0');
                while ((c = sr.ReadByte()) != ':')
                {
                    r += Convert.ToString(c - '0');
                }
                return ReadString(sr, Convert.ToInt32(r));
            }

            BtError e = new BtError
            {
                Message = $"Error: unknown BEncode item type: {c}"
            };

            return e;
        }

        public BtFile Load(string filename)
        {
            BtFile f = new BtFile();

            FileStream sr;
            try
            {
                sr = new FileStream(filename, FileMode.Open, FileAccess.Read);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }

            while (sr.Position < sr.Length)
            {
                f.Items.Add(ReadNext(sr));
            }

            sr.Close();

            return f;
        }
    }
}
