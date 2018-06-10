using TVRename.AppLogic.ScanItems.Items;

namespace TVRename.AppLogic.ScanItems.Actions
{
    /// <summary>
    /// An item in the scanned items listview that can be actioned.
    /// </summary>
    public abstract class ActionBase : ItemBase
    {
        private decimal _percent;
        public decimal PercentDone
        {
            get => ActionCompleted ? 100m : _percent;
            set => _percent = value;
        }

        /// <summary>
        /// Name of this action, e.g. "Copy", "Move", "Download"
        /// </summary>
        public abstract string ActionName { get; }

        /// <summary>
        /// Shortish text to display to user while task is running
        /// </summary>
        public abstract string ActionProgressText { get; }

        /// <summary>
        /// What does this action produce? typically a filename
        /// </summary>
        public abstract string ActionProduces { get; }

        /// <summary>
        /// For file copy/move, number of bytes in file.  for simple tasks, 1, or something proportional to how slow it is to copy files around.
        /// </summary>
        public abstract long ActionSizeOfWork { get; }

        /// <summary>
        /// Action the action.
        /// Do not return until done.
        /// Will be run in a dedicated thread.
        /// </summary>
        /// <param name="pause">Set to true to stop work until set back to false</param>
        /// <param name="statistics">Update the statistics once complete.</param>
        /// <returns></returns>
        public abstract bool PerformAction(ref bool pause, Statistics statistics);

        /// <summary>
        /// All work has been completed for this item, and can be removed from to-do list.  set to true on completion, even on error.
        /// </summary>
        public bool ActionCompleted { get; protected set; }

        /// <summary>
        /// Error state, after trying to do work?
        /// </summary>
        public bool ActionError { get; protected set; }

        /// <summary>
        /// Human-readable error message, for when Error is true
        /// </summary>
        public string ActionErrorText { get; protected set; }
    }
}

