using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TVRename.AppLogic.BitTorrent
{
    public class BtFile
    {
        public List<BtItemBase> Items { get; }

        public BtFile()
        {
            Items = new List<BtItemBase>();
        }

        public List<string> AllFilesInTorrent()
        {
            List<string> r = new List<string>();

            BtItemBase bti = GetItem("info");
            if (bti == null || bti.Type != BtChunk.Dictionary)
            {
                return null;
            }

            var infoDict = (BtDictionary)bti;
            bti = infoDict.GetItem("files");

            if (bti == null) // single file torrent
            {
                bti = infoDict.GetItem("name");
                if (bti == null || bti.Type != BtChunk.String)
                {
                    return null;
                }

                r.Add(((BtString)bti).AsString());
            }
            else
            {
                BtList fileList = (BtList)bti;

                foreach (BtItemBase itemBase in fileList.Items)
                {
                    BtDictionary file = (BtDictionary)itemBase;

                    BtItemBase thePath = file.GetItem("path");
                    if (thePath.Type != BtChunk.List)
                    {
                        return null;
                    }

                    BtList pathList = (BtList)thePath;
                    // want the last of the items in the list, which is the filename itself
                    int n = pathList.Items.Count - 1;
                    if (n < 0)
                    {
                        return null;
                    }

                    BtString fileName = (BtString)pathList.Items[n];
                    r.Add(fileName.AsString());
                }
            }

            return r;
        }

        public string AsText()
        {
            string res = "File= ";

            foreach (var btItemBase in Items)
            {
                res += btItemBase.AsText() + " ";
            }

            return res;
        }

        public BtItemBase GetItem(string key, bool ignoreCase = false)
        {
            return GetDict().GetItem(key, ignoreCase);
        }

        public BtDictionary GetDict()
        {
            Debug.Assert(Items.Count == 1);
            Debug.Assert(Items[0].Type == BtChunk.Dictionary);

            // our first (and only) Item will be a dictionary of stuff
            return (BtDictionary)Items[0];
        }

        public void Write(Stream sw)
        {
            foreach (BtItemBase btItemBase in Items)
            {
                btItemBase.Write(sw);
            }
        }
    }
}
