using System;
using System.Collections.Generic;
using System.Xml;

namespace TVRename.AppLogic.Helpers
{
    public static class XmlHelper
    {
        public static void WriteStringsToXml(List<string> strings, XmlWriter writer, string elementName, string stringName)
        {
            writer.WriteStartElement(elementName);
            foreach (string ss in strings)
            {
                writer.WriteStartElement(stringName);
                writer.WriteValue(ss);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        public static List<string> ReadStringsFromXml(XmlReader reader, string elementName, string stringName)
        {
            List<string> r = new List<String>();

            if (reader.Name != elementName)
                return r; // uhoh

            if (!reader.IsEmptyElement)
            {
                reader.Read();
                while (!reader.EOF)
                {
                    if ((reader.Name == elementName) && !reader.IsStartElement())
                        break;
                    if (reader.Name == stringName)
                        r.Add(reader.ReadElementContentAsString());
                    else
                        reader.ReadOuterXml();
                }
            }
            reader.Read();
            return r;
        }

        public static string ReadStringFixQuotesAndSpaces(XmlReader r)
        {
            string res = r.ReadElementContentAsString();
            res = res.Replace("\\'", "'");
            res = res.Replace("\\\"", "\"");
            res = res.Trim();
            return res;
        }

        public static void WriteElementToXML(XmlWriter writer, string elementName, string value, bool ignoreifBlank = false)
        {
            if (ignoreifBlank && string.IsNullOrEmpty(value)) return;

            writer.WriteStartElement(elementName);
            writer.WriteValue(value ?? "");
            writer.WriteEndElement();
        }
        public static void WriteElementToXML(XmlWriter writer, string elementName, double value)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(value);
            writer.WriteEndElement();
        }
        public static void WriteElementToXML(XmlWriter writer, string elementName, int value)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(value);
            writer.WriteEndElement();
        }
        public static void WriteElementToXML(XmlWriter writer, string elementName, bool value)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(value);
            writer.WriteEndElement();
        }
        public static void WriteElementToXML(XmlWriter writer, string attributeName, DateTime? value)
        {
            writer.WriteStartElement(attributeName);
            if (!(value == null))
                writer.WriteValue(value);
            writer.WriteEndElement();
        }

        public static void WriteAttributeToXML(XmlWriter writer, string attributeName, string value)
        {
            writer.WriteStartAttribute(attributeName);
            writer.WriteValue(value);
            writer.WriteEndAttribute();
        }
        public static void WriteAttributeToXML(XmlWriter writer, string attributeName, DateTime? value)
        {
            writer.WriteStartAttribute(attributeName);
            if (!(value == null))
                writer.WriteValue(value);
            writer.WriteEndAttribute();
        }
        public static void WriteAttributeToXML(XmlWriter writer, string attributeName, int value)
        {
            writer.WriteStartAttribute(attributeName);
            writer.WriteValue(value);
            writer.WriteEndAttribute();
        }
        public static void WriteAttributeToXML(XmlWriter writer, string attributeName, bool value)
        {
            writer.WriteStartAttribute(attributeName);
            writer.WriteValue(value);
            writer.WriteEndAttribute();
        }
        public static void WriteAttributeToXML(XmlWriter writer, string attributeName, long value)
        {
            writer.WriteStartAttribute(attributeName);
            writer.WriteValue(value);
            writer.WriteEndAttribute();
        }

        public static void WriteInfo(XmlWriter writer, string elemName, string attribute, string attributeVal, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                writer.WriteStartElement(elemName);
                if (!String.IsNullOrEmpty(attribute) && !String.IsNullOrEmpty(attributeVal))
                {
                    writer.WriteAttributeString(attribute, attributeVal);
                }
                writer.WriteValue(value);
                writer.WriteEndElement();
            }
        }

        public static void WriteInfo(XmlWriter writer, string elemName, string attribute, string attributeVal)
        {
            if (!string.IsNullOrEmpty(attributeVal))
            {
                writer.WriteStartElement(elemName);
                if (!String.IsNullOrEmpty(attribute) && !String.IsNullOrEmpty(attributeVal))
                {
                    writer.WriteAttributeString(attribute, attributeVal);
                }
                writer.WriteEndElement();
            }
        }
    }
}
