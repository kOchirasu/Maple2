using System.Xml;
using Maple2.File.IO;

namespace Maple2.File.Ingest.Utils;

public class StringTable {
    public readonly Dictionary<string, string> Table;

    public StringTable(M2dReader xmlReader) {
        Table = new Dictionary<string, string>();

        XmlDocument doc = xmlReader.GetXmlDocument(xmlReader.GetEntry("en/stringcommon.xml"));
        foreach (XmlNode node in doc.SelectNodes("ms2/key")) {
            string? id = node.Attributes?["id"]?.Value;
            string value = node.Attributes?["value"]?.Value ?? "";
            if (id == null) {
                continue;
            }

            Table[id] = value;
        }
    }
}
