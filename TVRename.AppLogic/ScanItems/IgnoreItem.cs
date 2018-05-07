using System.Xml;
using TVRename.AppLogic.Helpers;

namespace TVRename.AppLogic.ScanItems
{
    public class IgnoreItem
    {
        public string FileAndPath;

        public IgnoreItem(XmlReader r)
        {
            if (r.Name == "Ignore")
            {
                FileAndPath = r.ReadElementContentAsString();
            }
        }

        public IgnoreItem(string fileAndPath)
        {
            FileAndPath = fileAndPath;
        }

        public bool SameFileAs(IgnoreItem other)
        {
            if (string.IsNullOrEmpty(FileAndPath) || string.IsNullOrEmpty(other?.FileAndPath))
            {
                return false;
            }
            return FileAndPath == other.FileAndPath;
        }

        public void Write(XmlWriter writer)
        {
            XmlHelper.WriteElementToXML(writer, "Ignore", FileAndPath);
        }
    }
}
