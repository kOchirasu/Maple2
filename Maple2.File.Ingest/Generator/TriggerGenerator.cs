using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Xml;
using Maple2.File.Ingest.Utils;
using Maple2.File.IO;
using Maple2.File.IO.Crypto.Common;

namespace Maple2.File.Ingest.Generator;

public class TriggerGenerator {
    private readonly M2dReader reader;

    public TriggerGenerator(M2dReader xmlReader) {
        reader = xmlReader;
    }

    public void Generate() {
        foreach (PackFileEntry entry in reader.Files.Where(file => file.Name.StartsWith("trigger/"))) {
            string scriptDir = entry.Name.Split('/', StringSplitOptions.RemoveEmptyEntries)[1];
            string scriptName = Path.GetFileNameWithoutExtension(entry.Name);
            Directory.CreateDirectory($"trigger/{scriptDir}");
            string pyName = $"trigger/{scriptDir}/{scriptName}.py";
            using var stream = new StreamWriter(pyName);
            using var writer = new IndentedTextWriter(stream, "  ");
            writer.WriteLine(@$""""""" {entry.Name} """"""");

            XmlDocument document = reader.GetXmlDocument(entry);
            XmlNodeList stateNodeList = document.SelectNodes("ms2/state")!;
            try {
                foreach (XmlNode importNode in document.SelectNodes("ms2/import")!) {
                    string path = importNode.Attributes["path"].Value.ToLower();
                    string importDir = Directory.GetParent(path).Name;
                    string importName = Path.GetFileNameWithoutExtension(path);

                    writer.WriteLine("import common");
                    writer.WriteLine($"import {importDir}/{importName}");
                }
                writer.WriteLine();

                // Copy node list so that we can remove duplicate states
                List<XmlNode> stateNodes = stateNodeList.Cast<XmlNode>().ToList();
                var stateIndex = new Dictionary<string, short>();
                short index = 0;
                foreach (XmlNode stateNode in stateNodes.ToList()) {
                    string name = stateNode.Attributes["name"].Value;
                    if (stateIndex.ContainsKey(name)) {
                        Console.WriteLine($"Duplicate state in {entry.Name} ignored and removed: {name}");
                        stateNodes.Remove(stateNode);
                    } else {
                        stateIndex.Add(name, index++);
                    }
                }

                var script = new TriggerScript();
                foreach (XmlNode stateNode in stateNodes) {
                    TriggerScript.State scriptState = ParseState(stateNode, stateIndex, entry.Name);
                    script.States.Add(scriptState);
                }

                script.WriteTo(writer);
                Console.WriteLine($"Generated {pyName}...");
            } catch (Exception ex) {
                Console.WriteLine($"Failed to parse file: {entry.Name} - {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }

    private static TriggerScript.State ParseState(XmlNode node, Dictionary<string, short> stateIndex, string filePath) {
        string name = node.Attributes["name"].Value;
        var onEnter = new List<TriggerScript.Action>();
        foreach (XmlNode action in node.SelectNodes("onEnter/action")) {
            onEnter.Add(ParseAction(action));
        }

        var conditions = new List<TriggerScript.Condition>();
        foreach (XmlNode condition in node.SelectNodes("condition")) {
            conditions.Add(ParseCondition(condition, stateIndex, filePath));
        }

        var onExit = new List<TriggerScript.Action>();
        foreach (XmlNode action in node.SelectNodes("onExit/action")) {
            onExit.Add(ParseAction(action));
        }

        return new TriggerScript.State {
            Name = name,
            OnEnter = onEnter,
            Conditions = conditions,
            OnExit = onExit,
        };
    }

    private static TriggerScript.Action ParseAction(XmlNode node) {
        var args = new Dictionary<string, string>();
        foreach (XmlAttribute attribute in node.Attributes) {
            args[attribute.Name] = attribute.Value;
        }

        Debug.Assert(args.Remove("name", out string? name), "Unable to find name param");
        return new TriggerScript.Action {
            Name = name,
            Args = args,
        };
    }

    private static TriggerScript.Condition ParseCondition(XmlNode node, Dictionary<string, short> stateIndex, string filePath) {
        var args = new Dictionary<string, string>();
        foreach (XmlAttribute attribute in node.Attributes) {
            args[attribute.Name] = attribute.Value;
        }
        Debug.Assert(args.Remove("name", out string? name), "Unable to find name param");
        bool negated = name.StartsWith('!');

        var actions = new List<TriggerScript.Action>();
        foreach (XmlNode action in node.SelectNodes("action")) {
            actions.Add(ParseAction(action));
        }

        string? transition = node.SelectSingleNode("transition")?.Attributes?["state"]?.Value;
        // if (transition == null || !stateIndex.ContainsKey(transition)) {
        //     Console.WriteLine($"Script {filePath} Missing transition: {transition}");
        //     Console.WriteLine($"- {string.Join(",", stateIndex.Keys)}");
        //     transition = null;
        // }

        return new TriggerScript.Condition {
            Name = name.TrimStart('!'),
            Negated = negated,
            Args = args,
            Actions = actions,
            Transition = transition,
        };
    }
}
