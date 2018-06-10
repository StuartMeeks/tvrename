using TVRename.AppLogic.Delegates;
using TVRename.AppLogic.ScanItems;

namespace TVRename.AppLogic.Finders
{
    internal abstract class FinderBase
    {
        protected readonly TvRenameManager TvRenameManager;
        protected bool Cancel = false;

        public ItemList ActionList { get; internal set; }

        protected FinderBase(TvRenameManager tvRenameManager)
        {
            TvRenameManager = tvRenameManager;
        }
       
        public abstract void Check(ProgressUpdatedDelegate progressDelegate, int startPercent, int totalPercent);

        public abstract bool Active();

        public abstract FinderDisplayType DisplayType();

        public void Interrupt()
        {
            this.Cancel = true;
        }

        public void Reset()
        {
            this.Cancel = false;
        }

    }
}
