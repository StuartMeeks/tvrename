using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;

namespace TVRename.AppLogic
{
    [Serializable]
    public class ShowStatusColoringTypeList : Dictionary<ShowStatusColoringType, Color>
    {
        public ShowStatusColoringTypeList()
        {
        }
        protected ShowStatusColoringTypeList(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public bool IsShowStatusDefined(string showStatus)
        {
            foreach (KeyValuePair<ShowStatusColoringType, Color> e in this)
            {
                if (!e.Key.IsMetaType
                    && e.Key.IsShowLevel
                    && e.Key.Status.Equals(showStatus, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public Color GetEntry(bool meta, bool showLevel, string status)
        {
            foreach (KeyValuePair<ShowStatusColoringType, Color> e in this)
            {
                if (e.Key.IsMetaType == meta
                    && e.Key.IsShowLevel == showLevel
                    && e.Key.Status.Equals(status, StringComparison.CurrentCultureIgnoreCase))
                {
                    return e.Value;
                }
            }

            return Color.Empty;
        }
    }
}
