using System.Collections.Generic;
using System.Xml;
using TVRename.AppLogic.Helpers;

namespace TVRename.AppLogic.ProcessedItems
{
    public class Searchers
    {
        public string CurrentSearch;

        private List<SearcherChoice> _choices = new List<SearcherChoice>();

        public Searchers()
        {
            CurrentSearch = string.Empty;

            Add("Google", "https://www.google.com/search?q={ShowName}+S{Season:2}E{Episode}");
            Add("Pirate Bay", "https://thepiratebay.org/search/{ShowName} S{Season:2}E{Episode}");
            Add("binsearch", "https://www.binsearch.info/?q={ShowName}+S{Season:2}E{Episode}");

            CurrentSearch = "Google";
        }

        public Searchers(XmlReader reader)
        {
            _choices = new List<SearcherChoice>();
            CurrentSearch = "";

            reader.Read();
            if (reader.Name != "TheSearchers")
            {
                return;
            }

            reader.Read();
            while (!reader.EOF)
            {
                if (reader.Name == "TheSearchers" && !reader.IsStartElement())
                {
                    break;
                }

                if (reader.Name == "Current")
                {
                    CurrentSearch = reader.ReadElementContentAsString();
                }
                else if (reader.Name == "Choice")
                {
                    string url = reader.GetAttribute("URL");
                    if (url == null)
                    {
                        url = reader.GetAttribute("URL2");
                    }
                    else
                    {
                        // old-style URL, replace "!" with "{ShowName}+{Season}+{Episode}"
                        url = url.Replace("!", "{ShowName}+{Season}+{Episode}");
                    }
                    Add(reader.GetAttribute("Name"), url);
                    reader.ReadElementContentAsString();
                }
                else
                {
                    reader.ReadOuterXml();
                }
            }
        }

        public void SetToNumber(int n)
        {
            CurrentSearch = _choices[n].Name;
        }

        public int CurrentSearchNum()
        {
            return NumForName(CurrentSearch);
        }

        public int NumForName(string search)
        {
            for (int i = 0; i < _choices.Count; i++)
            {
                if (_choices[i].Name == search)
                {
                    return i;
                }
            }

            return 0;
        }

        public string CurrentSearchUrl()
        {
            if (_choices.Count == 0)
            {
                return "";
            }

            return _choices[CurrentSearchNum()].URL2;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("TheSearchers");
            XmlHelper.WriteElementToXML(writer, "Current", CurrentSearch);

            for (int i = 0; i < this.Count(); i++)
            {
                writer.WriteStartElement("Choice");
                XmlHelper.WriteAttributeToXML(writer, "Name", _choices[i].Name);
                XmlHelper.WriteAttributeToXML(writer, "URL2", _choices[i].URL2);
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); // TheSearchers
        }

        public void Clear()
        {
            _choices.Clear();
        }

        public void Add(string name, string url)
        {
            _choices.Add(new SearcherChoice {Name = name, URL2 = url});
        }

        public int Count()
        {
            return _choices.Count;
        }

        public string Name(int n)
        {
            if (n >= _choices.Count)
            {
                n = _choices.Count - 1;
            }
            else if (n < 0)
            {
                n = 0;
            }

            return _choices[n].Name;
        }

        public string Url(int n)
        {
            if (n >= _choices.Count)
            {
                n = _choices.Count - 1;
            }
            else if (n < 0)
            {
                n = 0;
            }

            return _choices[n].URL2;
        }
    }
}
