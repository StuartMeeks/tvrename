using System;
using System.Collections.Generic;
using TVRename.AppLogic.ScanItems;

namespace TVRename.AppLogic
{
    public class ActionItemQueue
    {
        /// <summary>
        /// The contents of the queue
        /// </summary>
        public List<ActionItemBase> ActionItems;

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
            ActionItems = new List<ActionItemBase>();
            ParallelLimit = parallelLimit;
            Name = name;
            ActionPosition = 0;
        }
    }
}
