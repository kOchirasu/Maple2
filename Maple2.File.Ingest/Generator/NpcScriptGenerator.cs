using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Text;
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

            foreach (TalkScript script in value.select) {
                Scripts[script.id] = script;
            }
            if (value.job != null) {
                Scripts[value.job.id] = value.job;
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
            bool isLast = index == script.content.Count - 1;

            // Roulette Npcs have this information in their scripts.
            if (content.buttonSet != 0) {
                return (NpcTalkButton) content.buttonSet;
            }

            // Job scripts can have special buttons
            if (isLast && id == Value.job?.id) {
                switch (Npc.basic.kind) {
                    case >= 30 and < 40:
                        return NpcTalkButton.SelectableBeauty;
                    case 80:
                        return NpcTalkButton.ChangeJob;
                    case 81:
                        return NpcTalkButton.PenaltyResolve;
                    case 82:
                        return NpcTalkButton.TakeBoat;
                }
            }

            if (content.distractor.Count > 0) {
                return NpcTalkButton.SelectableDistractor;
            }

            return isLast ? NpcTalkButton.Close : NpcTalkButton.Next;
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
            BlankLine(writer);

            writer.WriteLine("/// <summary>");
            writer.WriteLine($"/// {id}: {npcName}");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public class _{id} : NpcScript {{");
            writer.Indent++;

            GenerateFirst(writer, index);
            BlankLine(writer);

            GenerateSelect(writer, index.Value);
            BlankLine(writer);

            writer.WriteLine("protected override int Execute(int selection) {");
            writer.Indent++;
            writer.WriteLine("switch (Id, Index++) {");
            writer.Indent++;

            // foreach (TalkScript talkScript in data.select) {
            //     GenerateCase(talkScript.id, writer, index, talkScript);
            // }

            if (data.job != null) {
                GenerateCase(data.job.id, writer, index, data.job);
            }

            foreach (TalkScript talkScript in data.script) {
                GenerateCase(talkScript.id, writer, index, talkScript);
            }

            writer.Indent--;
            writer.WriteLine("}");
            BlankLine(writer);
            writer.WriteLine("return default;");
            writer.Indent--;
            writer.WriteLine("}");
            BlankLine(writer);

            GenerateButton(writer, index);
            writer.Indent--;
            writer.WriteLine("}");

            writer.Flush();
            stream.Flush();
        }
    }

    public void GenerateEvent() {
        foreach ((int id, NpcScript data) in scriptParser.ParseNpc()) {
            // No scripts for this npc
            if (data.script.Count == 0 && data.job == null) {
                continue;
            }
            (string npcName, NpcData? npc) = npcs.GetValueOrDefault(id);
            if (npc == null) {
                // The Npc is not included in FeatureLocale, so skip generating the script.
                continue;
            }

            var index = new IndexedNpcScript(data, npc);
            var str = new StringWriter();
            var writer = new IndentedTextWriter(str, "    ");
            writer.WriteLine("/// <summary>");
            writer.WriteLine($"/// {id}: {npcName}");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static class Event{id} {{");
            writer.Indent++;
            bool generated = GenerateEvent(writer, index);
            if (!generated) {
                continue;
            }
            writer.Indent--;
            writer.WriteLine("}");

            writer.Flush();
            str.Flush();

            var stream = new StreamWriter($"Scripts/NpcEvent/{id}.cs");
            stream.Write(str);
            stream.Flush();
        }
    }

    private void GenerateFirst(IndentedTextWriter writer, IndexedNpcScript index) {
        Dictionary<int, TalkScript> randomPicks = index.Value.script.Where(script => script.randomPick)
            .ToDictionary(entry => entry.id, entry => entry);

        writer.WriteLine($"protected override int First() {{");
        writer.Indent++;
        if (index.Value.job != null && randomPicks.Count == 0) {
            writer.WriteLine($"return {index.Value.job.id};");
        } else if (index.Value.job == null && randomPicks.Count == 1) {
            writer.WriteLine($"return {randomPicks.First().Key};");
        } else {
            if (index.Value.job != null) {
                writer.WriteLine($"// TODO: Job {index.Value.job.id}");
            }
            if (randomPicks.Count > 0) {
                writer.WriteLine($"// TODO: RandomPick {string.Join(";", randomPicks.Keys)}");
            }
        }
        writer.Indent--;
        writer.WriteLine("}");
    }

    private void GenerateSelect(IndentedTextWriter writer, NpcScript script) {
        int[] ids = script.select.Select(select => select.id).ToArray();
        foreach (TalkScript talk in script.select) {
            writer.WriteLine($"// Select {talk.id}:");
            foreach (Content content in talk.content) {
                WriteScriptText(writer, content.text);
            }
        }

        if (script.select.Count > 1) {
            writer.WriteLine($"protected override int Select() => Random({string.Join(", ", ids)});");
        } else {
            writer.WriteLine($"protected override int Select() => {ids.FirstOrDefault()};");
        }
    }

    private void GenerateButton(IndentedTextWriter writer, IndexedNpcScript index) {
        writer.WriteLine("protected override NpcTalkButton Button() {");
        writer.Indent++;
        writer.WriteLine("return (Id, Index) switch {");
        writer.Indent++;
        foreach (TalkScript script in index.Value.script) {
            for (int i = 0; i < script.content.Count; i++) {
                int id = script.id;
                writer.WriteLine($"({id}, {i}) => NpcTalkButton.{index.GetResult(id, i)},");
            }
        }
        if (index.Value.job != null) {
            for (int i = 0; i < index.Value.job.content.Count; i++) {
                int id = index.Value.job.id;
                writer.WriteLine($"({id}, {i}) => NpcTalkButton.{index.GetResult(id, i)},");
            }
        }
        writer.WriteLine("_ => NpcTalkButton.None,");
        writer.Indent--;
        writer.WriteLine("};");
        writer.Indent--;
        writer.WriteLine("}");
    }

    private void GenerateCase(int id, IndentedTextWriter writer, IndexedNpcScript index, TalkScript script) {
        if (script.content.Count == 1) {
            writer.WriteLine($"case ({id}, 0):");
            writer.Indent++;
            Content content = script.content[0];
            GenerateContent(writer, content);
            writer.WriteLine("return -1;");
            writer.Indent--;
            return;
        }

        for (int i = 0; i < script.content.Count; i++) {
            writer.WriteLine($"case ({id}, {i}):");
            writer.Indent++;
            Content content = script.content[i];
            GenerateContent(writer, content);

            // If it's the last content, we need to transition.
            bool isLast = i == script.content.Count - 1;
            writer.WriteLine(isLast ? "return -1;" : $"return {script.id};");
            writer.Indent--;
        }
    }

    private void GenerateContent(IndentedTextWriter writer, Content content) {
        var builder = new StringBuilder();
        if (content.functionID > 0) {
            builder.Append($"functionID={content.functionID} ");
        }
        // if (content.buttonSet > 0) {
        //     builder.Append($"buttonSet={content.buttonSet} ");
        // }
        if (content.openTalkReward) {
            builder.Append($"openTalkReward={content.openTalkReward} ");
        }
        if (builder.Length > 0) {
            writer.WriteLine($"// {builder}");
        }

        WriteScriptText(writer, content.text);

        if (content.distractor.Count > 0) {
            writer.WriteLine("switch (selection) {");
            writer.Indent++;
            for (int j = 0; j < content.distractor.Count; j++) {
                Distractor distractor = content.distractor[j];
                WriteScriptText(writer, distractor.text);
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
                        writer.WriteLine($"return {gotoId};");
                    } else {
                        writer.WriteLine($"// TODO: goto {success}");
                        if (!string.IsNullOrWhiteSpace(fail)) {
                            writer.WriteLine($"// TODO: gotoFail {fail}");
                            writer.WriteLine($"return {fail};");
                        } else {
                            writer.WriteLine("return -1;");
                        }
                    }
                }
                writer.Indent--;
            }

            writer.Indent--;
            writer.WriteLine("}");
        }
    }

    private bool GenerateEvent(IndentedTextWriter writer, IndexedNpcScript index) {
        List<TalkScript> eventScripts = index.Value.script.Where(script => {
            return script.content.Count > 0 && script.content.Any(content => content.@event.Count > 0);
        }).ToList();

        if (eventScripts.Count == 0) {
            return false;
        }

        writer.WriteLine("public static (string, string, string) Event(int scriptId, int eventId) {");
        writer.Indent++;
        writer.WriteLine("return (scriptId, eventId) switch {");
        writer.Indent++;
        foreach (TalkScript script in eventScripts) {
            Content content = script.content.First();
            foreach (Event @event in content.@event) {
                if (@event.content.Count == 1) {
                    Content eventContent = @event.content.First();
                    WriteScriptText(writer, eventContent.text, false);
                    writer.WriteLine($"({script.id}, {@event.id}) => (\"{eventContent.text}\", \"{eventContent.voiceID}\", \"{eventContent.illust}\"),");
                } else {
                    writer.WriteLine($"({script.id}, {@event.id}) => Random(");
                    writer.Indent++;
                    for (int i = 0; i < @event.content.Count; i++) {
                        Content eventContent = @event.content[i];
                        WriteScriptText(writer, eventContent.text, false);
                        writer.Write($"(\"{eventContent.text}\", \"{eventContent.voiceID}\", \"{eventContent.illust}\")");
                        writer.WriteLine((i < script.content.Count - 1) ? "," : "");
                    }
                    writer.Indent--;
                    writer.WriteLine("),");
                }
            }
        }
        writer.WriteLine("_ => (\"\", \"\", \"\"),");
        writer.Indent--;
        writer.WriteLine("};");
        writer.Indent--;
        writer.WriteLine("}");
        return true;
    }

    private void WriteScriptText(IndentedTextWriter writer, string scriptText, bool writeTextId = true) {
        if (writeTextId) {
            writer.WriteLine($"// {scriptText}");
        }
        bool first = true;
        foreach (string text in GetScriptText(scriptText)) {
            if (first) {
                writer.WriteLine($"// - {text}");
                first = false;
            } else {
                writer.WriteLine($"//   {text}");
            }
        }
    }

    private string[] GetScriptText(string scriptText) {
        string id = scriptText[8..^1];
        string? result = strings.GetValueOrDefault(id);
        return result == null ? new []{""} : result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
    }

    private static void BlankLine(IndentedTextWriter writer) {
        int indent = writer.Indent;
        writer.Indent = 0;
        writer.WriteLine();
        writer.Indent = indent;
    }
}
