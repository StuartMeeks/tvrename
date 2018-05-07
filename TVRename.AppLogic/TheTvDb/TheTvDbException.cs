using System;

namespace TVRename.AppLogic.TheTvDb
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown if an error occurs in the XML when reading TheTVDB.xml
    /// </summary>
    [Serializable()]
    public class TheTvDbException : Exception
    {
        public TheTvDbException(string message)
            : base(message)
        {
        }
    }

}
