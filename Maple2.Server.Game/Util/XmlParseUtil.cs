using System.Collections.Generic;
using System.Xml;

namespace Maple2.Server.Game.Util;

public static class XmlParseUtil {
    public static Dictionary<string, string> GetParameters(string? xmlString) {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        XmlDocument xmlParameter = new();
        xmlParameter.LoadXml(xmlString);
        foreach (XmlNode? node in xmlParameter) {
            foreach (XmlAttribute attribute in node?.Attributes) {
                parameters.Add(attribute.Name, attribute.Value);
            }
        }
        return parameters;
    }
}
