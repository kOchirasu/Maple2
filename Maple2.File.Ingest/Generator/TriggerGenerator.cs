using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using Maple2.File.Ingest.Utils;
using Maple2.File.IO;
using Maple2.File.IO.Crypto.Common;
using static System.Char;

namespace Maple2.File.Ingest.Generator;

public class TriggerGenerator {
    private readonly M2dReader reader;
    private readonly Dictionary<string, string> checkUserCountStates = new();
    private readonly Dictionary<string, string> checkUser10States = new();

    public TriggerGenerator(M2dReader xmlReader) {
        reader = xmlReader;

        XmlDocument checkUserCountDocument = reader.GetXmlDocument(reader.Files.First(entry =>
            entry.Name.StartsWith("trigger/dungeon_common/checkusercount.xml")));
        foreach (XmlNode stateNode in checkUserCountDocument.SelectNodes("ms2/state")) {
            string stateName = stateNode.Attributes["name"].Value;
            checkUserCountStates.Add(stateName, stateName);
        }

        XmlDocument guildCheckUserDocument = reader.GetXmlDocument(reader.Files.First(entry =>
            entry.Name.StartsWith("trigger/dungeon_common/checkuser10_guildraid.xml")));
        foreach (XmlNode stateNode in guildCheckUserDocument.SelectNodes("ms2/state")) {
            string stateName = stateNode.Attributes["name"].Value;
            checkUser10States.Add(stateName, stateName);
        }
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
                writer.WriteLine("from common import *");
                writer.WriteLine("import state");

                var stateIndex = new Dictionary<string, string>();
                foreach (XmlNode importNode in document.SelectNodes("ms2/import")!) {
                    string path = importNode.Attributes["path"].Value.ToLower();
                    string importModule = Directory.GetParent(path).Name;
                    string importName = Path.GetFileNameWithoutExtension(path);

                    writer.WriteLine($"from {importModule}.{importName} import *");
                    switch (importName) {
                        case "checkusercount":
                            foreach (KeyValuePair<string, string> state in checkUserCountStates) {
                                stateIndex.Add(state.Key, state.Value);
                            }
                            break;
                        case "checkuser10_guildraid":
                            foreach (KeyValuePair<string, string> state in checkUser10States) {
                                stateIndex.Add(state.Key, state.Value);
                            }
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown import: {importModule}/{importName}");
                    }
                }
                stateIndex.Add("DungeonStart", "state.DungeonStart");
                writer.WriteLine();

                // Copy node list so that we can remove duplicate states
                List<XmlNode> stateNodes = stateNodeList.Cast<XmlNode>().ToList();
                foreach (XmlNode stateNode in stateNodes.ToList()) {
                    string name = stateNode.Attributes["name"].Value;
                    if (name == "DungeonStart") continue; // Special case

                    name = FixClassName(name);
                    if (stateIndex.ContainsKey(name)) {
                        Console.WriteLine($"Duplicate state in {entry.Name} ignored and removed: {name}");
                        stateNodes.Remove(stateNode);
                    } else {
                        stateIndex.Add(name, name);
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

        // Create module for dungeon_common
        System.IO.File.Create("trigger/dungeon_common/__init__.py");
    }

    private static TriggerScript.State ParseState(XmlNode node, Dictionary<string, string> stateIndex, string filePath) {
        string name = FixClassName(node.Attributes["name"].Value);
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

    private static TriggerScript.Condition ParseCondition(XmlNode node, Dictionary<string, string> stateIndex, string filePath) {
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

        string? transition = FixClassName(node.SelectSingleNode("transition")?.Attributes?["state"]?.Value);
        if (transition != null) {
            if (stateIndex.TryGetValue(transition, out string result)) {
                transition = result;
            } else {
                Console.WriteLine($"Script {filePath} Missing transition: {transition}");
                Console.WriteLine($"- {string.Join(",", stateIndex.Keys)}");
                transition = null;
            }
        }

        return new TriggerScript.Condition {
            Name = name.TrimStart('!'),
            Negated = negated,
            Args = args,
            Actions = actions,
            Transition = transition,
            TransitionComment = FixClassName(node.SelectSingleNode("transition")?.Attributes?["state"]?.Value),
        };
    }

    private static readonly Dictionary<string, string> SubStart = new() {
        {"1st", "First"},
        {"2nd", "Second"},
        {"3rd", "Third"},
        {"4th", "Fourth"},
        {"5th", "Fifth"},
        {"6th", "Sixth"},
        {"7th", "Seventh"},
    };
    [return: NotNullIfNotNull("name")]
    private static string? FixClassName(string? name) {
        if (name == null) {
            return null;
        }

        name = name.Replace("-", "To");
        foreach ((string key, string value) in SubStart) {
            if (name.StartsWith(key)) {
                name = name.Replace(key, value);
                break;
            }
        }

        string prefix = "";
        while (name.Length > 0 && !IsLetter(name[0])) {
            prefix += name[0];
            name = name[1..];
        }

        // name is already valid
        if (prefix.Length == 0) {
            return name;
        }
        if (name.Length == 0) {
            return $"State{prefix}";
        }

        return !IsLetter(name[^1]) ? $"{name}_{prefix}" : $"{name}{prefix}";
    }
}
