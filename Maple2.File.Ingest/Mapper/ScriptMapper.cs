﻿using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Script;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using CinematicContent = Maple2.Model.Metadata.CinematicContent;
using CinematicDistractor = Maple2.Model.Metadata.CinematicDistractor;
using CinematicEventScript = Maple2.Model.Metadata.CinematicEventScript;
using ScriptContent = Maple2.Model.Metadata.ScriptContent;

namespace Maple2.File.Ingest.Mapper;

public class ScriptMapper : TypeMapper<ScriptMetadata> {
    private readonly ScriptParser parser;

    public ScriptMapper(M2dReader xmlReader) {
        parser = new ScriptParser(xmlReader);
    }

    protected override IEnumerable<ScriptMetadata> Map() {
        foreach ((int id, NpcScript script) in parser.ParseNpc()) {
            var states = new Dictionary<int, ScriptState>();
            if (script.job != null) {
                states.Add(script.job.id, new ScriptState(
                    Id: script.job.id,
                    Type: ScriptStateType.Job,
                    RandomPick: false,
                    Contents: ParseCinematicContents(script.job.content)));
            }
            foreach (TalkScript select in script.select) {
                states.Add(select.id, new ScriptState(
                    Id: select.id,
                    Type: ScriptStateType.Select,
                    RandomPick: false,
                    Contents: ParseCinematicContents(select.content)));
            }
            foreach (ConditionTalkScript select in script.script) {
                int[] conditions = select.gotoConditionTalkID; // TODO:
                states.Add(select.id, new ScriptState(
                    Id: select.id,
                    Type: ScriptStateType.Script,
                    RandomPick: select.randomPick,
                    Contents: ParseCinematicContents(select.content)));
            }
            if (states.Count == 0) {
                continue;
            }

            yield return new ScriptMetadata(Id: id, Type: ScriptType.Npc, States: states);
        }

        foreach ((int id, QuestScript script) in parser.ParseQuest()) {
            var states = new  Dictionary<int, ScriptState>();
            foreach (QuestTalkScript talk in script.script) {
                states.Add(talk.id, new ScriptState(
                    Id: talk.id,
                    Type: ScriptStateType.Select,
                    RandomPick: false,
                    Contents: ParseCinematicContents(talk.content)));
            }
            if (states.Count == 0) {
                continue;
            }

            yield return new ScriptMetadata(Id: id, Type: ScriptType.Quest, States: states);
        }
    }

    private static CinematicContent[] ParseCinematicContents(IList<Parser.Xml.Script.CinematicContent> contents) {
        var result = new List<CinematicContent>();
        foreach (Parser.Xml.Script.CinematicContent content in contents) {
            var distractors = new List<CinematicDistractor>();
            foreach (Parser.Xml.Script.CinematicDistractor distractor in content.distractor) {
                distractors.Add(new CinematicDistractor(Goto: distractor.@goto, GotoFail: distractor.gotoFail));
            }

            var events = new List<CinematicEventScript>();
            foreach (Parser.Xml.Script.CinematicEventScript @event in content.@event) {
                var eventContents = new List<ScriptContent>();
                foreach (Parser.Xml.Script.ScriptContent eventContent in @event.content) {
                    eventContents.Add(new ScriptContent(
                        Text: eventContent.text,
                        VoiceId: eventContent.voiceID,
                        Illustration: eventContent.illust));
                }
                events.Add(new CinematicEventScript(@event.id, eventContents.ToArray()));
            }

            result.Add(new CinematicContent(
                Text: content.text,
                ButtonType: (NpcTalkButton) content.buttonSet,
                FunctionId: content.functionID,
                Distractors: distractors.ToArray(),
                Events: events.ToArray()));
        }

        return result.ToArray();
    }
}
