using System;
using System.IO;
using System.Text;

namespace TVRename.AppLogic.BitTorrent
{
    public class BtString : BtItemBase
    {
        public byte[] Data { get; set; }

        public BtString(string s)
            : base(BtChunk.String)
        {
            SetString(s);
        }

        public BtString()
            : base(BtChunk.String)
        {
            Data = new byte[0];
        }

        public void SetString(string s)
        {
            Data = Encoding.UTF8.GetBytes(s);
        }

        public override string AsText()
        {
            return $"String={AsString()}";
        }

        public string AsString()
        {
            var encoding = Encoding.UTF8;
            return encoding.GetString(Data);
        }

        public byte[] StringTwentyBytePiece(int pieceNum)
        {
            var res = new byte[20];
            if (pieceNum * 20 + 20 > Data.Length)
            {
                return null;
            }

            Array.Copy(Data, pieceNum * 20, res, 0, 20);
            return res;
        }

        public static string CharsToHex(byte[] data, int start, int n)
        {
            var r = string.Empty;
            for (var i = 0; i < n; i++)
            {
                r += (data[start + i] < 16 ? "0" : string.Empty) + data[start + i].ToString("x").ToUpper();
            }

            return r;
        }

        public string PieceAsNiceString(int pieceNum)
        {
            return CharsToHex(Data, pieceNum * 20, 20);
        }

        public override void Write(Stream sw)
        {
            // Byte strings are encoded as follows: <string length encoded in base ten ASCII>:<string data>
            var len = Encoding.ASCII.GetBytes(Data.Length.ToString());
            sw.Write(len, 0, len.Length);
            sw.WriteByte((byte)':');
            sw.Write(Data, 0, Data.Length);
        }
    }
}
