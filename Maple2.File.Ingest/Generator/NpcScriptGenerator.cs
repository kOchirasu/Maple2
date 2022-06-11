using System.CodeDom.Compiler;
using System.Diagnostics;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Npc;
using Maple2.File.Parser.Xml.Quest;
using Maple2.File.Parser.Xml.Script;
using Maple2.Model.Enum;
using Maple2.Tools.Extensions;

namespace Maple2.File.Ingest.Generator;

public class NpcScriptGenerator {
    private readonly Dictionary<int, (string Name, NpcData Data)> npcs;
    private readonly Dictionary<int, (string Name, QuestData Data)> quests;
    private readonly IDictionary<string, string?> strings;
    private readonly ScriptParser scriptParser;

    public NpcScriptGenerator(M2dReader xmlReader) {
        npcs = new NpcParser(xmlReader).Parse()
            .ToDictionary(entry => entry.Id, entry => (entry.Name, entry.Data));
        quests = new QuestParser(xmlReader).Parse()
            .ToDictionary(entry => entry.Id, entry => (entry.Name, entry.Data));

        scriptParser = new ScriptParser(xmlReader);
        strings = scriptParser.ParseStrings();
    }

    private class IndexedNpcScript {
        public readonly NpcScript Value;
        public readonly NpcData Npc;
        public IDictionary<int, TalkScript> Scripts;

        public IndexedNpcScript(NpcScript value, NpcData npc) {
            Value = value;
            Npc = npc;
            Scripts = new Dictionary<int, TalkScript>();

            if (value.job != null) {
                Scripts[value.job.id] = value.job;
            }
            foreach (TalkScript script in value.select) {
                Scripts[script.id] = script;
            }
            foreach (TalkScript script in value.script) {
                Scripts[script.id] = script;
            }
        }

        public NpcTalkButton GetResult(int id, int index) {
            if (!Scripts.TryGetValue(id, out TalkScript? script)) {
                return NpcTalkButton.None;
            }

            Debug.Assert(index < script.content.Count, $"{index} >= {script.content.Count}");
            Content content = script.content[index];
            if (content.distractor.Count > 0) {
                int kind = Npc.basic.kind;
                if (kind is >= 30 and < 40) {
                    return NpcTalkButton.SelectableBeauty;
                }
                return NpcTalkButton.SelectableDistractor;
            }

            return index == script.content.Count - 1 ? NpcTalkButton.Close : NpcTalkButton.Next;
        }
    }

    public void Generate() {
        foreach ((int id, NpcScript data) in scriptParser.ParseNpc()) {
            // No scripts for this npc
            if (data.script.Count == 0 && data.job == null) {
                //Console.WriteLine($"Skipping: {id}");
                continue;
            }

            //Console.WriteLine($"Processing: {id}");

            (string npcName, NpcData? npc) = npcs.GetValueOrDefault(id);
            if (npc == null) {
                // The Npc is not included in FeatureLocale, so skip generating the script.
                continue;
            }

            var index = new IndexedNpcScript(data, npc);
            var stream = new StreamWriter($"Scripts/Npc/{id}.cs");
            var writer = new IndentedTextWriter(stream, "    ");
            writer.WriteLine("using Maple2.Model.Enum;");
            writer.WriteLine("using Maple2.Script.Npc;");
            writer.WriteLine();
            writer.WriteLine("/// <summary>");
            writer.WriteLine($"/// {id}: {npcName}");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public class _{id} : NpcScript {{");
            writer.Indent++;
            writer.WriteLine($"protected override (int, NpcTalkButton) FirstScript() {{");
            writer.Indent++;
            GenerateFirstScript(id, writer, index);
            writer.Indent--;
            writer.WriteLine("}");
            writer.Indent--;
            writer.WriteLine();
            writer.Indent++;
            writer.WriteLine("protected override (int, NpcTalkButton) Next(int selection) {");
            writer.Indent++;
            writer.WriteLine("switch (Id, Index++) {");
            writer.Indent++;

            foreach (TalkScript talkScript in data.select) {
                GenerateCase(talkScript.id, writer, index, talkScript);
            }

            if (data.job != null) {
                GenerateCase(data.job.id, writer, index, data.job);
            }

            foreach (TalkScript talkScript in data.script) {
                GenerateCase(talkScript.id, writer, index, talkScript);
            }

            writer.Indent--;
            writer.WriteLine("}");
            writer.WriteLine();
            writer.WriteLine("return default;");
            writer.Indent--;
            writer.WriteLine("}");
            writer.Indent--;
            writer.WriteLine("}");

            writer.Flush();
            stream.Flush();
        }
    }

    private void GenerateFirstScript(int id, IndentedTextWriter writer, IndexedNpcScript index) {
        Dictionary<int, TalkScript> randomPicks = index.Value.script.Where(script => script.randomPick)
            .ToDictionary(entry => entry.id, entry => entry);
        if (index.Value.job != null && randomPicks.Count == 0) {
            int key = index.Value.job.id;
            writer.WriteLine($"return ({key}, NpcTalkButton.{index.GetResult(key, 0)});");
            return;
        }
        if (index.Value.job == null && randomPicks.Count == 1) {
            int key = randomPicks.First().Key;
            writer.WriteLine($"return ({key}, NpcTalkButton.{index.GetResult(key, 0)});");
            return;
        }

        if (index.Value.job != null) {
            int key = index.Value.job.id;
            writer.WriteLine($"// TODO: Job {key}");
            writer.WriteLine($"// return ({key}, NpcTalkButton.{index.GetResult(key, 0)});");
        }
        if (randomPicks.Count > 0) {
            writer.WriteLine($"// TODO: RandomPick {string.Join(";", randomPicks.Keys)}");
            foreach (int key in randomPicks.Keys) {
                writer.WriteLine($"// return ({key}, NpcTalkButton.{index.GetResult(key, 0)});");
            }
        }
    }

    private void GenerateCase(int id, IndentedTextWriter writer, IndexedNpcScript index, TalkScript script) {
        if (script.content.Count == 1) {
            writer.WriteLine($"case ({id}, 0):");
            writer.Indent++;
            Content content = script.content[0];
            GenerateContent(writer, index, script, content);
            writer.WriteLine("return default;");
            writer.Indent--;
            return;
        }

        int i = 0;
        for (; i < script.content.Count; i++) {
            writer.WriteLine($"case ({id}, {i}):");
            writer.Indent++;
            Content content = script.content[i];
            GenerateContent(writer, index, script, content);

            // If it's the last content, we need to transition.
            if (i == script.content.Count - 1) {
                writer.WriteLine("return default;");
            } else {
                writer.WriteLine($"return ({script.id}, NpcTalkButton.{index.GetResult(script.id, i+1)});");
            }
            writer.Indent--;
        }
    }

    private void GenerateContent(IndentedTextWriter writer, IndexedNpcScript index, TalkScript script, Content content) {
        writer.Write($"// {content.text} ");
        if (content.functionID > 0) {
            writer.Write($"functionID={content.functionID} ");
        }
        if (content.buttonSet > 0) {
            writer.Write($"buttonSet={content.buttonSet} ");
        }
        writer.WriteLine();
        bool first = true;
        foreach (string text in GetScriptText(content.text)) {
            if (first) {
                writer.WriteLine($"// - {text}");
                first = false;
            } else {
                writer.WriteLine($"//   {text}");
            }
        }

        if (content.distractor.Count > 0) {
            writer.WriteLine("switch (selection) {");
            writer.Indent++;
            for (int j = 0; j < content.distractor.Count; j++) {
                Distractor distractor = content.distractor[j];
                writer.WriteLine($"// {distractor.text}");
                first = true;
                foreach (string text in GetScriptText(distractor.text)) {
                    if (first) {
                        writer.WriteLine($"// - {text}");
                        first = false;
                    } else {
                        writer.WriteLine($"//   {text}");
                    }
                }
                writer.WriteLine($"case {j}:");
                writer.Indent++;

                string success = distractor.@goto;
                string fail = distractor.gotoFail;
                // No goto means script is complete

                if (string.IsNullOrWhiteSpace(success)) {
                    // You can't fail if no success?
                    Debug.Assert(string.IsNullOrWhiteSpace(fail));
                    writer.WriteLine("return default;");
                } else {
                    if (string.IsNullOrWhiteSpace(fail) && int.TryParse(success, out int gotoId)) {
                        if (gotoId == 0) {
                            writer.WriteLine("return (0, NpcTalkButton.None);");
                        } else {
                            writer.WriteLine($"return ({gotoId}, NpcTalkButton.{index.GetResult(gotoId, 0)});");
                        }
                    } else {
                        writer.WriteLine($"// TODO: goto {success}");
                        foreach (int key in success.Split(",").Select(int.Parse)) {
                            if (key == 0) {
                                writer.WriteLine("return (0, NpcTalkButton.None);");
                            } else {
                                writer.WriteLine($"// (Id, Button) = ({key}, NpcTalkButton.{index.GetResult(key, 0)});");
                            }
                        }
                        writer.WriteLine($"// TODO: gotoFail {fail}");
                        if (int.TryParse(fail, out int failKey)) {
                            writer.WriteLine($"// (Id, Button) = ({failKey}, NpcTalkButton.{index.GetResult(failKey, 0)});");
                        }
                        writer.WriteLine("return (0, NpcTalkButton.None);");
                    }
                }
                writer.Indent--;
            }

            writer.Indent--;
            writer.WriteLine("}");
        }
    }

    private string[] GetScriptText(string scriptText) {
        string id = scriptText[8..^1];
        string? result = strings.GetValueOrDefault(id);
        if (result == null) {
            return new []{""};
        }
        return result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
    }
}
