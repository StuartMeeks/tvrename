using System.Collections.Generic;
using TVRename.AppLogic.ScanItems.Actions;

namespace TVRename.AppLogic
{
    public class ActionQueue
    {
        /// <summary>
        /// The contents of the queue
        /// </summary>
        public List<ActionBase> ActionItems;

        /// <summary>
        /// The number of concurrent tasks
        /// </summary>
        public int ParallelLimit;

        /// <summary>
        /// The name of the queue
        /// </summary>
        public string Name;

        /// <summary>
        /// The position of the next item in the queue
        /// </summary>
        public int ActionPosition;

        public ActionQueue(string name, int parallelLimit)
        {
            ActionItems = new List<ActionBase>();
            ParallelLimit = parallelLimit;
            Name = name;
            ActionPosition = 0;
        }
    }
}
