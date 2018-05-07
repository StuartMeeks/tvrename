using System.Xml;
using TVRename.AppLogic.Helpers;

namespace TVRename.AppLogic.ProcessedItems
{
    public class SeasonRule
    {
        public int First;
        public int Second;
        public RuleAction Action;
        public string UserSuppliedText;

        public SeasonRule()
        {
            SetToDefaults();
        }

        public SeasonRule(XmlReader reader)
        {
            SetToDefaults();

            reader.Read();
            while (reader.Name != "Rule")
            {
                return;
            }

            reader.Read();
            while (reader.Name != "Rule")
            {
                if (reader.Name == "DoWhatNow")
                {
                    Action = (RuleAction)reader.ReadElementContentAsInt();
                }
                else if (reader.Name == "First")
                {
                    First = reader.ReadElementContentAsInt();
                }
                else if (reader.Name == "Second")
                {
                    Second = reader.ReadElementContentAsInt();
                }
                else if (reader.Name == "Text")
                {
                    UserSuppliedText = reader.ReadElementContentAsString();
                }
                else
                {
                    reader.ReadOuterXml();
                }
            }
        }

        public SeasonRule(SeasonRule other)
        {
            Action = other.Action;
            First = other.First;
            Second = other.Second;
            UserSuppliedText = other.UserSuppliedText;
        }

        public override string ToString()
        {
            return $"Season Rule: {ActionInWords()} with parameters {First}, {Second} and user supplied text: {UserSuppliedText}";
        }

        public void SetToDefaults()
        {
            Action = RuleAction.IgnoreEp;
            First = Second = -1;
            UserSuppliedText = "";
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Rule");
            XmlHelper.WriteElementToXML(writer, "DoWhatNow", (int)Action);
            XmlHelper.WriteElementToXML(writer, "First", First);
            XmlHelper.WriteElementToXML(writer, "Second", Second);
            XmlHelper.WriteElementToXML(writer, "Text", UserSuppliedText);
            writer.WriteEndElement(); // Rule
        }

        public string ActionInWords()
        {
            switch (Action)
            {
                case RuleAction.IgnoreEp:
                    return "Ignore";
                case RuleAction.Remove:
                    return "Remove";
                case RuleAction.Collapse:
                    return "Collapse";
                case RuleAction.Swap:
                    return "Swap";
                case RuleAction.Merge:
                    return "Merge";
                case RuleAction.Split:
                    return "Split";
                case RuleAction.Insert:
                    return "Insert";
                case RuleAction.Rename:
                    return "Rename";
                default:
                    return "<Unknown>";
            }
        }
    }
}
