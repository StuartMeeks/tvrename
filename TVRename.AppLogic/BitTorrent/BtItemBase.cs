using System.IO;

namespace TVRename.AppLogic.BitTorrent
{
    public abstract class BtItemBase
    {
        public BtChunk Type { get; }

        protected BtItemBase(BtChunk type)
        {
            Type = type;
        }

        public virtual string AsText()
        {
            return $"Type ={Type.ToString()}";
        }

        public abstract void Write(Stream sw);
    }
}
