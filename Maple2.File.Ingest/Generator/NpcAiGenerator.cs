using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using M2dXmlGenerator;
using Maple2.File.Ingest.Utils;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Enum;
using Maple2.File.Parser.Xml.AI;

namespace Maple2.File.Ingest.Generator;

public class NpcAiGenerator {
    private readonly Dictionary<string, NpcAi> ais;
    private static readonly AiScriptCommon ApiScript = new();

    public NpcAiGenerator(M2dReader serverReader) {
        ais = new AiParser(serverReader, true).Parse()
            .ToDictionary(entry => entry.AiName, entry => entry.Data);
    }

    public void Generate() {
        foreach ((string name, NpcAi ai) in ais) {
            string path = $"Scripts/Ai/{name.Substring(0, name.LastIndexOf('.'))}.py";
            // Console.WriteLine($"Generating {path}");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var stream = new StreamWriter(path);
            var writer = new IndentedTextWriter(stream, "    ");
            writer.WriteLine("from ai_api import Ai");
            if (ai.Battle.Count > 0 || ai.BattleEnd.Count > 0) {
                writer.WriteLine("from typing import override");
            }
            BlankLine(writer);
            BlankLine(writer);

            writer.WriteLine("class Main(Ai):");
            writer.Indent++;

            if (ai.Battle.Count > 0) {
                writer.WriteLine("@override");
                writer.WriteLine("def battle(self) -> None:");
                writer.Indent++;
                GenerateEntries(writer, ai.Battle);
                writer.Indent--;
                BlankLine(writer);
            }

            if (ai.BattleEnd.Count > 0) {
                writer.WriteLine("@override");
                writer.WriteLine("def battle_end(self) -> None:");
                writer.Indent++;
                GenerateEntries(writer, ai.BattleEnd);
                writer.Indent--;
                BlankLine(writer);
            }

            if (ai.AiPresets.Count > 0) {
                foreach (Entry entry in ai.AiPresets) {
                    GenerateEntry(writer, entry);
                }
            }

            writer.Indent--;

            writer.Flush();
            stream.Flush();
        }

        using var apiStream = new StreamWriter("Scripts/Ai/ai_api.py");
        using var apiWriter = new IndentedTextWriter(apiStream, "    ");
        ApiScript.WriteTo(apiWriter);
        Console.WriteLine("API Script: Scripts/Ai/ai_api.py");
    }

    private static void WriteComment(IndentedTextWriter writer, Comment comment) {
        foreach (string line in AiTranslate.Translate(comment.Value).Trim().Split(new []{"\r\n", "\n"}, StringSplitOptions.None)) {
            writer.WriteLine($"# {line}");
        }
    }

    private static void GenerateEntries(IndentedTextWriter writer, List<Entry> entries) {
        List<Entry>.Enumerator it = entries.GetEnumerator();
        if (!it.MoveNext()) {
            return;
        }

        if (it.Current is Comment) {
            writer.WriteLine(@""""""""); // Python block comment start
            bool first = true;
            do {
                if (it.Current is not Comment comment) {
                    break;
                }

                // Include empty line between individual comment nodes
                if (first) {
                    first = false;
                } else {
                    writer.WriteLine();
                }

                foreach (string line in AiTranslate.Translate(comment.Value).Trim().Split(new []{"\r\n", "\n"}, StringSplitOptions.None)) {
                    writer.WriteLine(line);
                }
            } while (it.MoveNext());
            writer.WriteLine(@""""""""); // Python block comment end
            BlankLine(writer);
        }

        do {
            GenerateEntry(writer, it.Current);
        } while (it.MoveNext());
    }

    private static void GenerateEntry(IndentedTextWriter writer, Entry entry, bool isRoot = false) {
        switch (entry) {
            case NodeEntry node:
                WriteNode(writer, node);
                return;
            case ConditionEntry condition:
                WriteCondition(writer, condition);
                return;
            case AiPresetEntry preset:
                if (isRoot) {
                    writer.WriteLine($"def {TriggerTranslate.ToSnakeCase(preset.name)}(self) -> None:");
                    writer.Indent++;
                    foreach (Entry child in preset.Entries) {
                        GenerateEntry(writer, child);
                    }
                    writer.Indent--;
                    BlankLine(writer);
                } else {
                    writer.WriteLine($"self.{TriggerTranslate.ToSnakeCase(preset.name)}()");
                }
                return;
            case Comment comment:
                WriteComment(writer, comment);
                return;
        }
    }

    #region Arg Writers
    private static bool WriteArg(StringBuilder writer, string name, string value, string defaultValue = "") {
        if (!string.IsNullOrWhiteSpace(value) && value != defaultValue) {
            writer.Append($"{name}='{value}'");
            return true;
        }
        return false;
    }

    private static bool WriteArg(StringBuilder writer, string name, long value, long defaultValue = 0) {
        if (value != defaultValue) {
            writer.Append($"{name}={value}");
            return true;
        }
        return false;
    }

    private static bool WriteArg(StringBuilder writer, string name, float value, float defaultValue = 0f) {
        if (Math.Abs(value - defaultValue) > 0.000001f) {
            writer.Append($"{name}={MathF.Round(value, 3)}");
            return true;
        }
        return false;
    }

    private static bool WriteArg(StringBuilder writer, string name, bool value, bool defaultValue = false) {
        if (value != defaultValue) {
            string valueStr = value ? "True" : "False";
            writer.Append($"{name}={valueStr}");
            return true;
        }
        return false;
    }

    private static bool WriteArg(StringBuilder writer, string name, Vector3 value, Vector3 defaultValue = default) {
        if (value != defaultValue) {
            writer.Append($"{name}=[{MathF.Round(value.X, 3)}, {MathF.Round(value.Y, 3)}, {MathF.Round(value.Z, 3)}]");
            return true;
        }
        return false;
    }

    private static bool WriteArg(StringBuilder writer, string name, string[] value) {
        if (value.Length > 0) {
            writer.Append($"{name}=[{string.Join(", ", value.Select(v => $"'{v}'"))}]");
            return true;
        }
        return false;
    }

    private static bool WriteArg(StringBuilder writer, string name, int[] value) {
        if (value.Length > 0) {
            writer.Append($"{name}=[{string.Join(", ", value)}]");
            return true;
        }
        return false;
    }
    #endregion

    private static void WriteEntryArgs(StringBuilder writer, Entry entry, params string[] names) {
        foreach (string name in names) {
            switch (entry) {
                case NodeEntry node:
                    WriteNodeArg(writer, node, name);
                    break;
                case ConditionEntry condition:
                    WriteConditionArg(writer, condition, name);
                    break;
            }
        }
    }

    private static void WriteNodeArg(StringBuilder writer, dynamic entry, string name) {
        switch (name) {
            case "prob":
                if (entry.name == "select") {
                    if (entry.prob.Length != 1 || entry.prob[0] != 100) {
                        writer.Append($"{name}=[{string.Join(", ", entry.prob)}], ");
                    }
                } else {
                    if (WriteArg(writer, name, entry.prob, 100)) {
                        writer.Append(", ");
                    }
                }
                return;
        }

        dynamic value = name switch {
            "limit" => entry.limit,
            "skill_idx" => entry.skillIdx,
            "animation" => entry.animation,
            "speed" => entry.speed,
            "till" => entry.till,
            "initial_cooltime" => entry.initialCooltime,
            "cooltime" => entry.cooltime,
            "is_keep_battle" => entry.isKeepBattle,
            "idx" => entry.idx,
            "level" => entry.level,
            "sequence" => entry.sequence,
            "face_pos" => entry.facePos,
            "face_target_tick" => entry.faceTargetTick,
            "pos" => entry.pos,
            "face_target" => entry.faceTarget,
            "key" => entry.key,
            "value" => entry.value,
            "type" => entry.type.ToString(),
            "rank" => entry.rank,
            "additional_id" => entry.additionalId,
            "additional_level" => entry.additionalLevel,
            "from_pos" => entry.from,
            "to_pos" => entry.to,
            "center" => entry.center,
            "target" => entry.target == AiTarget.defaultTarget ? "" : entry.target.ToString(),
            "no_change_when_no_target" => entry.noChangeWhenNoTarget,
            "message" => entry.message,
            "duration_tick" => entry.durationTick,
            "delay_tick" => entry.delayTick,
            "is_modify" => entry.isModify,
            "height_multiplier" => entry.heightMultiplier,
            "use_npc_prob" => entry.useNpcProb,
            "destination" => entry.destination,
            "npc_id" => entry.npcId,
            "npc_count_max" => entry.npcCountMax,
            "npc_count" => entry.npcCount,
            "life_time" => entry.lifeTime,
            "summon_rot" => entry.summonRot,
            "summon_pos" => entry.summonPos,
            "summon_pos_offset" => entry.summonPosOffset,
            "summon_target_offset" => entry.summonTargetOffset,
            "summon_radius" => entry.summonRadius,
            "group" => entry.group,
            "master" => entry.master == SummonMaster.Master ? "" : entry.master.ToString(),
            "option" => ((SummonOption[]) entry.option).Select(e => e.ToString()).ToArray(),
            "trigger_id" => entry.triggerID,
            "is_ride_off" => entry.isRideOff,
            "ride_npc_ids" => entry.rideNpcIDs,
            "is_random" => entry.isRandom,
            "hp_percent" => entry.hpPercent,
            "id" => entry.id,
            "is_target" => entry.isTarget,
            "effect_name" => entry.effectName,
            "group_id" => entry.groupID,
            "illust" => entry.illust,
            "duration" => entry.duration,
            "script" => entry.script,
            "sound" => entry.sound,
            "voice" => entry.voice,
            "height" => entry.height,
            "radius" => entry.radius,
            "time_tick" => entry.timeTick,
            "is_show_effect" => entry.isShowEffect,
            "normal" => entry.normal,
            "reactable" => entry.reactable,
            "interact_id" => entry.interactID,
            "kfm_name" => entry.kfmName,
            "random_room_id" => entry.randomRoomID,
            "portal_duration" => entry.portalDuration,
            _ => throw new ArgumentException($"Unsupported node argument: {name}"),
        };

        if (WriteArg(writer, name, value)) {
            writer.Append(", ");
        }
    }

    private static void WriteConditionArg(StringBuilder writer, dynamic condition, string name) {
        dynamic value = name switch {
            "use_summon_group" => condition.useSummonGroup,
            "summon_group" => condition.summonGroup,
            "key" => condition.key,
            "is_keep_battle" => condition.isKeepBattle,
            "battle_time_begin" => condition.battleTimeBegin,
            "battle_time_loop" => condition.battleTimeLoop,
            "battle_time_end" => condition.battleTimeEnd,
            "skill_idx" => condition.skillIdx,
            "skill_lev" => condition.skillLev,
            "target_state" => condition.targetState.ToString(),
            "id" => condition.id,
            "level" => condition.level,
            "overlap_count" => condition.overlapCount,
            "is_target" => condition.isTarget,
            _ => throw new ArgumentException($"Unsupported condition argument: {name}"),
        };

        if (WriteArg(writer, name, value)) {
            writer.Append(", ");
        }
    }

    private static void WriteNode(IndentedTextWriter writer, NodeEntry node) {
        var builder = new StringBuilder();
        bool nestedNodes = node.Entries.Count > 0 && node is not SelectNode;
        if (nestedNodes) {
            builder.Append("if ");
        }
        switch (node) {
            case SelectNode select:
                builder.Append("choice = self.select(");
                WriteEntryArgs(builder, node, "prob", "use_npc_prob");
                builder.Append(')');
                builder.Replace(", )", ")");
                writer.WriteLine(builder.ToString());

                // if (select.useNpcProb) {
                //     Console.WriteLine(builder);
                // }

                // Debug.Assert(node.useNpcProb || node.prob.Length >= node.node.Count);
                int probIndex = 0;
                bool finished = false;
                for (int i = 0; i < select.Entries.Count; i++) {
                    if (select.Entries[i] is Comment comment) {
                        GenerateEntry(writer, comment);
                        continue;
                    }

                    // Recursively generate child nodes
                    if (!select.useNpcProb) {
                        if (probIndex == 0) {
                            writer.WriteLine($"if choice == {probIndex}: # prob={select.prob[probIndex]}");
                        } else {
                            if (probIndex < select.prob.Length && select.prob[probIndex] > 0) {
                                writer.WriteLine($"elif choice == {probIndex}: # prob={select.prob[probIndex]}");
                            } else {
                                if (finished) {
                                    Console.WriteLine($"select nodes exhausted: [{string.Join(",", select.prob)}]");
                                    break;
                                }
                                writer.WriteLine("else:");
                                finished = true;
                            }
                        }
                    }
                    writer.Indent++;
                    GenerateEntry(writer, select.Entries[i]);
                    writer.Indent--;
                    probIndex++;
                }
                return;
            case TraceNode trace:
                builder.Append("self.trace(");
                WriteEntryArgs(builder, trace, "limit", "skill_idx", "animation", "speed", "till", "initial_cooltime", "cooltime", "is_keep_battle");
                builder.Append(')');
                break;
            case SkillNode skill:
                builder.Append("self.skill(");
                WriteEntryArgs(builder, skill, "idx", "id", "level", "prob", "sequence", "face_pos", "face_target", "face_target_tick", "initial_cooltime", "cooltime", "limit", "is_keep_battle");
                builder.Append(')');
                break;
            case TeleportNode teleport:
                builder.Append("self.teleport(");
                WriteEntryArgs(builder, teleport, "pos", "prob", "face_pos", "face_target", "initial_cooltime", "cooltime", "is_keep_battle");
                builder.Append(')');
                break;
            case StandbyNode standby:
                builder.Append("self.standby(");
                WriteEntryArgs(builder, standby, "limit", "prob", "animation", "face_pos", "face_target", "initial_cooltime", "cooltime", "is_keep_battle");
                builder.Append(')');
                break;
            case SetDataNode setData:
                builder.Append("self.set_data(");
                WriteEntryArgs(builder, setData, "key", "value", "cooltime");
                builder.Append(')');
                break;
            case TargetNode target:
                builder.Append("self.target(");
                WriteEntryArgs(builder, target, "type", "prob", "rank", "additional_id", "additional_level", "from_pos", "to_pos", "center", "target", "no_change_when_no_target", "initial_cooltime", "cooltime", "is_keep_battle");
                builder.Append(')');
                break;
            case SayNode say:
                builder.Append("self.say(");
                WriteEntryArgs(builder, say, "message", "prob", "duration_tick", "delay_tick", "initial_cooltime", "cooltime", "is_keep_battle");
                builder.Append(')');
                break;
            case SetValueNode setValue:
                builder.Append("self.set_value(");
                WriteEntryArgs(builder, setValue, "key", "value", "initial_cooltime", "cooltime", "is_modify", "is_keep_battle");
                builder.Append(')');
                break;
            case ConditionsNode conditions:
                builder.Append("self.conditions(");
                WriteEntryArgs(builder, conditions, "initial_cooltime", "cooltime", "is_keep_battle");
                builder.Append(')');
                break;
            case JumpNode jump:
                builder.Append("self.jump(");
                WriteEntryArgs(builder, jump, "pos", "speed", "height_multiplier", "type", "cooltime", "is_keep_battle");
                builder.Append(')');
                break;
            case MoveNode move:
                builder.Append("self.move(");
                WriteEntryArgs(builder, move, "destination", "prob", "animation", "limit", "speed", "face_target", "initial_cooltime", "cooltime", "is_keep_battle");
                builder.Append(')');
                break;
            case SummonNode summon:
                builder.Append("self.summon(");
                WriteEntryArgs(builder, summon, "npc_id", "npc_count_max", "npc_count", "delay_tick", "life_time", "summon_rot", "summon_pos", "summon_pos_offset", "summon_target_offset", "summon_radius", "group", "master", "option", "cooltime", "is_keep_battle");
                builder.Append(')');
                break;
            case HideVibrateAllNode hideVibrateAll:
                builder.Append("self.hide_vibrate_all(");
                WriteEntryArgs(builder, hideVibrateAll, "is_keep_battle");
                builder.Append(')');
                break;
            case TriggerSetUserValueNode triggerSetUserValue:
                builder.Append("self.trigger_set_user_value(");
                WriteEntryArgs(builder, triggerSetUserValue, "trigger_id", "key", "value", "cooltime", "is_keep_battle");
                builder.Append(')');
                break;
            case RideNode ride:
                builder.Append("self.ride(");
                WriteEntryArgs(builder, ride, "type", "is_ride_off", "ride_npc_ids");
                builder.Append(')');
                break;
            case SetSlaveValueNode setSlaveValue:
                builder.Append("self.set_slave_value(");
                WriteEntryArgs(builder, setSlaveValue, "key", "value", "is_random", "cooltime", "is_modify", "is_keep_battle");
                builder.Append(')');
                break;
            case SetMasterValueNode setMasterValue:
                builder.Append("self.set_master_value(");
                WriteEntryArgs(builder, setMasterValue, "key", "value", "is_random", "cooltime", "is_modify", "is_keep_battle");
                builder.Append(')');
                break;
            case RunawayNode runaway:
                builder.Append("self.runaway(");
                WriteEntryArgs(builder, runaway, "animation", "skill_idx", "till", "limit", "face_pos", "initial_cooltime", "cooltime");
                builder.Append(')');
                break;
            case MinimumHpNode minimumHp:
                builder.Append("self.minimum_hp(");
                WriteEntryArgs(builder, minimumHp, "hp_percent");
                builder.Append(')');
                break;
            case BuffNode buff:
                builder.Append("self.buff(");
                WriteEntryArgs(builder, buff, "id", "level", "type", "prob", "initial_cooltime", "cooltime", "is_keep_battle");
                builder.Append(')');
                break;
            case TargetEffectNode targetEffect:
                builder.Append("self.target_effect(");
                WriteEntryArgs(builder, targetEffect, "effect_name");
                builder.Append(')');
                break;
            case ShowVibrateNode showVibrate:
                builder.Append("self.show_vibrate(");
                WriteEntryArgs(builder, showVibrate, "group_id");
                builder.Append(')');
                break;
            case SidePopupNode sidePopup:
                builder.Append("self.side_popup(");
                WriteEntryArgs(builder, sidePopup, "type", "illust", "duration", "script", "sound", "voice");
                builder.Append(')');
                break;
            case SetValueRangeTargetNode setValueRangeTarget:
                builder.Append("self.set_value_range_target(");
                WriteEntryArgs(builder, setValueRangeTarget, "key", "value", "height", "radius", "cooltime", "is_modify", "is_keep_battle");
                builder.Append(')');
                break;
            case AnnounceNode announce:
                builder.Append("self.announce(");
                WriteEntryArgs(builder, announce, "message", "duration_tick", "cooltime");
                builder.Append(')');
                break;
            case ModifyRoomTimeNode modifyRoomTime:
                builder.Append("self.modify_room_time(");
                WriteEntryArgs(builder, modifyRoomTime, "time_tick", "is_show_effect");
                builder.Append(')');
                break;
            case TriggerModifyUserValueNode triggerModifyUserValue:
                builder.Append("self.trigger_modify_user_value(");
                WriteEntryArgs(builder, triggerModifyUserValue, "trigger_id", "key", "value");
                builder.Append(')');
                break;
            case RemoveSlavesNode removeSlaves:
                builder.Append("self.remove_slaves(");
                WriteEntryArgs(builder, removeSlaves, "is_keep_battle");
                builder.Append(')');
                break;
            case CreateRandomRoomNode createRandomRoom:
                builder.Append("self.create_random_room(");
                WriteEntryArgs(builder, createRandomRoom, "random_room_id", "portal_duration");
                builder.Append(')');
                break;
            case CreateInteractObjectNode createInteractObject:
                builder.Append("self.create_interact_object(");
                WriteEntryArgs(builder, createInteractObject, "interact_id", "life_time", "kfm_name", "normal", "reactable");
                builder.Append(')');
                break;
            case RemoveMeNode:
                builder.Append("self.remove_me()");
                break;
            case SuicideNode:
                builder.Append("self.suicide()");
                break;
            default:
                throw new ArgumentException($"Invalid Node: {node.name}");
        }

        builder.Replace(", )", ")");
        if (nestedNodes) {
            builder.Append(':');
        }
        writer.WriteLine(builder.ToString());

        writer.Indent++;
        foreach (Entry child in node.Entries) {
            GenerateEntry(writer, child);
        }
        writer.Indent--;
    }

    private static void WriteCondition(IndentedTextWriter writer, ConditionEntry condition) {
        Debug.Assert(condition.Entries.Count > 0, "Condition has no elements");

        bool? conditional = null;
        var builder = new StringBuilder();
        switch (condition) {
            case DistanceLessCondition distanceLess:
                builder.Append($"if self.distance() < {distanceLess.value}:");
                break;
            case DistanceOverCondition distanceOver:
                builder.Append($"if self.distance() > {distanceOver.value}:");
                break;
            case HpLessCondition hpLess:
                builder.Append($"if self.hp() < {hpLess.value}:");
                break;
            case HpOverCondition hpOver:
                builder.Append($"if self.hp() > {hpOver.value}:");
                break;
            case SlaveCountCondition slaveCount:
                builder.Append("if self.slave_count(");
                WriteEntryArgs(builder, slaveCount, "use_summon_group", "summon_group");

                string op = slaveCount.slaveCountOp switch {
                    ConditionOp.Equal => "=",
                    ConditionOp.Greater => ">",
                    ConditionOp.Less => "<",
                    ConditionOp.GreaterEqual => ">=",
                    ConditionOp.LessEqual => "<=",
                    _ => throw new ArgumentException("Invalid Op"),
                };
                builder.Append($" {op} {slaveCount.slaveCount}:");
                break;
            case ExtraDataCondition extraData:
                builder.Append("if self.extra_data(");
                WriteEntryArgs(builder, extraData, "key", "is_keep_battle");
                builder.Append($") = {extraData.value}:");
                break;
            case CombatTimeCondition combatTime:
                builder.Append("if self.combat_time(");
                WriteEntryArgs(builder, combatTime, "battle_time_begin", "battle_time_loop", "battle_time_end");
                builder.Append("):");
                break;
            case SkillRangeCondition skillRange:
                builder.Append("if self.skill_range(");
                WriteEntryArgs(builder, skillRange, "skill_idx", "skill_lev", "is_keep_battle");
                builder.Append("):");
                break;
            case StateCondition state:
                builder.Append("if self.state(");
                WriteEntryArgs(builder, state, "target_state");
                builder.Append("):");
                break;
            case AdditionalCondition additional:
                builder.Append("if self.additional(");
                WriteEntryArgs(builder, additional, "id", "level", "overlap_count", "is_target");
                builder.Append("):");
                break;
            case FeatureCondition feature:
                conditional = FeatureLocaleFilter.FeatureEnabled(feature.feature);
                builder.Append($"# Feature {feature.feature} enabled: {conditional}");
                // if (FeatureLocaleFilter.FeatureEnabled(feature.feature)) {
                //     builder.Append($"if True: # {feature.feature}");
                // } else {
                //     builder.Append($"if False: # {feature.feature}");
                // }
                break;
            case TrueCondition:
                conditional = true;
                builder.Append("# if True:");
                break;
            default:
                throw new ArgumentException($"Invalid Condition: {condition.name}");
        }

        builder.Replace(", )", ")");
        writer.WriteLine(builder.ToString());

        if (conditional == null) {
            writer.Indent++;
        }
        if (conditional is null or true) {
            foreach (Entry child in condition.Entries) {
                GenerateEntry(writer, child);
            }
        }
        if (conditional == null) {
            writer.Indent--;
        }
    }

    private static void BlankLine(IndentedTextWriter writer) {
        int indent = writer.Indent;
        writer.Indent = 0;
        writer.WriteLine();
        writer.Indent = indent;
    }
}
