﻿using System.Globalization;
using Maple2.Database.Extensions;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Enum;
using Maple2.File.Parser.Xml.Table.Server;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using ExpType = Maple2.Model.Enum.ExpType;
using InstanceType = Maple2.Model.Enum.InstanceType;
using JobConditionTable = Maple2.Model.Metadata.JobConditionTable;

namespace Maple2.File.Ingest.Mapper;

public class ServerTableMapper : TypeMapper<ServerTableMetadata> {
    private readonly ServerTableParser parser;

    public ServerTableMapper(M2dReader xmlReader) {
        parser = new ServerTableParser(xmlReader);
    }

    protected override IEnumerable<ServerTableMetadata> Map() {
        yield return new ServerTableMetadata { Name = "instancefield.xml", Table = ParseInstanceField() };
        yield return new ServerTableMetadata { Name = "*scriptCondition.xml", Table = ParseScriptCondition() };
        yield return new ServerTableMetadata { Name = "*scriptFunction.xml", Table = ParseScriptFunction() };
        yield return new ServerTableMetadata { Name = "jobConditionTable.xml", Table = ParseJobCondition() };
        yield return new ServerTableMetadata { Name = "bonusGame*.xml", Table = ParseBonusGameTable() };
        yield return new ServerTableMetadata { Name = "globalItemDrop*.xml", Table = ParseGlobalItemDropTable() };
        yield return new ServerTableMetadata { Name = "userStat*.xml", Table = ParseUserStat() };
        yield return new ServerTableMetadata { Name = "individualItemDrop.xml", Table = ParseIndividualItemDropTable() };
        yield return new ServerTableMetadata { Name = "adventureExpTable.xml", Table = ParsePrestigeExpTable() };

    }

    private InstanceFieldTable ParseInstanceField() {
        var results = new Dictionary<int, InstanceFieldMetadata>();
        foreach ((int instanceId, Parser.Xml.Table.Server.InstanceField instanceField) in parser.ParseInstanceField()) {
            foreach (int fieldId in instanceField.fieldIDs) {

                InstanceFieldMetadata instanceFieldMetadata = new(
                    MapId: fieldId,
                    Type: (InstanceType) instanceField.instanceType,
                    InstanceId: instanceId,
                    BackupSourcePortal: instanceField.backupSourcePortal,
                    PoolCount: instanceField.poolCount,
                    SaveField: instanceField.isSaveField,
                    NpcStatFactorId: instanceField.npcStatFactorID,
                    MaxCount: instanceField.maxCount,
                    OpenType: instanceField.openType,
                    OpenValue: instanceField.openValue
                );

                results.Add(fieldId, instanceFieldMetadata);
            }
        }

        return new InstanceFieldTable(results);
    }

    private ScriptConditionTable ParseScriptCondition() {
        var results = new Dictionary<int, Dictionary<int, ScriptConditionMetadata>>();
        results = MergeNpcScriptConditions(results, parser.ParseNpcScriptCondition());
        results = MergeQuestScriptConditions(results, parser.ParseQuestScriptCondition());

        return new ScriptConditionTable(results);
    }

    private Dictionary<int, Dictionary<int, ScriptConditionMetadata>> MergeNpcScriptConditions(Dictionary<int, Dictionary<int, ScriptConditionMetadata>> results, IEnumerable<(int NpcId, IDictionary<int, Parser.Xml.Table.Server.NpcScriptCondition> ScriptConditions)> parser) {
        foreach ((int npcId, IDictionary<int, Parser.Xml.Table.Server.NpcScriptCondition> scripts) in parser) {
            var scriptConditions = new Dictionary<int, ScriptConditionMetadata>();
            foreach ((int scriptId, Parser.Xml.Table.Server.NpcScriptCondition scriptCondition) in scripts) {
                var questStarted = new Dictionary<int, bool>();
                foreach (string quest in scriptCondition.quest_start) {
                    KeyValuePair<int, bool> parsedQuest = ParseToKeyValuePair(quest);
                    questStarted.Add(parsedQuest.Key, parsedQuest.Value);
                }

                var questsCompleted = new Dictionary<int, bool>();
                foreach (string quest in scriptCondition.quest_complete) {
                    KeyValuePair<int, bool> parsedQuest = ParseToKeyValuePair(quest);
                    questsCompleted.Add(parsedQuest.Key, parsedQuest.Value);
                }

                var items = new List<KeyValuePair<ItemComponent, bool>>();
                for (int i = 0; i < scriptCondition.item.Length; i++) {
                    KeyValuePair<int, bool> parsedItem = ParseToKeyValuePair(scriptCondition.item[i]);
                    string itemCount = scriptCondition.itemCount.ElementAtOrDefault(i) ?? "1";
                    if (!int.TryParse(itemCount, out int itemAmount)) {
                        itemAmount = 1;
                    }
                    var item = new ItemComponent(parsedItem.Key, -1, itemAmount, ItemTag.None);
                    items.Add(new KeyValuePair<ItemComponent, bool>(item, parsedItem.Value));
                }

                scriptConditions.Add(scriptId, new ScriptConditionMetadata(
                    Id: npcId,
                    ScriptId: scriptId,
                    Type: ScriptType.Npc,
                    MaidAuthority: scriptCondition.maid_auth,
                    MaidExpired: scriptCondition.maid_expired != "!1",
                    MaidReadyToPay: scriptCondition.maid_ready_to_pay != "!1",
                    MaidClosenessRank: scriptCondition.maid_affinity_grade,
                    MaidClosenessTime: ParseToKeyValuePair(scriptCondition.maid_affinity_time),
                    MaidMoodTime: ParseToKeyValuePair(scriptCondition.maid_mood_time),
                    MaidDaysBeforeExpired: ParseToKeyValuePair(scriptCondition.maid_day_before_expired),
                    JobCode: scriptCondition.job?.Select(job => (JobCode) job).ToList() ?? [],
                    QuestStarted: questStarted,
                    QuestCompleted: questsCompleted,
                    Items: items,
                    Buff: ParseToKeyValuePair(scriptCondition.buff),
                    Meso: ParseToKeyValuePair(scriptCondition.meso),
                    Level: ParseToKeyValuePair(scriptCondition.level),
                    AchieveCompleted: ParseToKeyValuePair(scriptCondition.achieve_complete),
                    InGuild: scriptCondition.guild
                ));
            }
            results.Add(npcId, scriptConditions);
        }
        return results;
    }

    private Dictionary<int, Dictionary<int, ScriptConditionMetadata>> MergeQuestScriptConditions(Dictionary<int, Dictionary<int, ScriptConditionMetadata>> results, IEnumerable<(int NpcId, IDictionary<int, Parser.Xml.Table.Server.QuestScriptCondition> ScriptConditions)> parser) {
        foreach ((int questId, IDictionary<int, Parser.Xml.Table.Server.QuestScriptCondition> scripts) in parser) {
            var scriptConditions = new Dictionary<int, ScriptConditionMetadata>();
            foreach ((int scriptId, Parser.Xml.Table.Server.QuestScriptCondition scriptCondition) in scripts) {
                var questStarted = new Dictionary<int, bool>();
                foreach (string quest in scriptCondition.quest_start) {
                    KeyValuePair<int, bool> parsedQuest = ParseToKeyValuePair(quest);
                    questStarted.Add(parsedQuest.Key, parsedQuest.Value);
                }

                var questsCompleted = new Dictionary<int, bool>();
                foreach (string quest in scriptCondition.quest_complete) {
                    KeyValuePair<int, bool> parsedQuest = ParseToKeyValuePair(quest);
                    questsCompleted.Add(parsedQuest.Key, parsedQuest.Value);
                }

                var items = new List<KeyValuePair<ItemComponent, bool>>();
                for (int i = 0; i < scriptCondition.item.Length; i++) {
                    KeyValuePair<int, bool> parsedItem = ParseToKeyValuePair(scriptCondition.item[i]);
                    string itemCount = scriptCondition.itemCount.ElementAtOrDefault(i) ?? "1";
                    if (!int.TryParse(itemCount, out int itemAmount)) {
                        itemAmount = 1;
                    }
                    var item = new ItemComponent(parsedItem.Key, -1, itemAmount, ItemTag.None);
                    items.Add(new KeyValuePair<ItemComponent, bool>(item, parsedItem.Value));
                }

                scriptConditions.Add(scriptId, new ScriptConditionMetadata(
                    Id: questId,
                    ScriptId: scriptId,
                    Type: ScriptType.Quest,
                    MaidAuthority: scriptCondition.maid_auth,
                    MaidExpired: scriptCondition.maid_expired != "!1",
                    MaidReadyToPay: scriptCondition.maid_ready_to_pay != "!1",
                    MaidClosenessRank: scriptCondition.maid_affinity_grade,
                    MaidClosenessTime: ParseToKeyValuePair(scriptCondition.maid_affinity_time),
                    MaidMoodTime: ParseToKeyValuePair(scriptCondition.maid_mood_time),
                    MaidDaysBeforeExpired: ParseToKeyValuePair(scriptCondition.maid_day_before_expired),
                    JobCode: scriptCondition.job?.Select(job => (JobCode) job).ToList() ?? [],
                    QuestStarted: questStarted,
                    QuestCompleted: questsCompleted,
                    Items: items,
                    Buff: ParseToKeyValuePair(scriptCondition.buff),
                    Meso: ParseToKeyValuePair(scriptCondition.meso),
                    Level: ParseToKeyValuePair(scriptCondition.level),
                    AchieveCompleted: ParseToKeyValuePair(scriptCondition.achieve_complete),
                    InGuild: scriptCondition.guild
                ));
            }
            results.Add(questId, scriptConditions);
        }
        return results;
    }

    private static KeyValuePair<int, bool> ParseToKeyValuePair(string input) {
        bool value = !input.StartsWith("!");

        if (!value) {
            input = input.Replace("!", "");
        }

        if (!int.TryParse(input, out int key)) {
            key = 0;
        }
        return new KeyValuePair<int, bool>(key, value);
    }

    private ScriptFunctionTable ParseScriptFunction() {
        var results = new Dictionary<int, Dictionary<int, Dictionary<int, ScriptFunctionMetadata>>>();
        results = MergeNpcScriptFunctions(results, parser.ParseNpcScriptFunction());
        results = MergeQuestScriptFunctions(results, parser.ParseQuestScriptFunction());

        return new ScriptFunctionTable(results);
    }

    private static Dictionary<int, Dictionary<int, Dictionary<int, ScriptFunctionMetadata>>> MergeNpcScriptFunctions(Dictionary<int, Dictionary<int, Dictionary<int, ScriptFunctionMetadata>>> results, IEnumerable<(int NpcId, IDictionary<int, Parser.Xml.Table.Server.NpcScriptFunction> ScriptFunctions)> parser) {
        foreach ((int npcId, IDictionary<int, Parser.Xml.Table.Server.NpcScriptFunction> scripts) in parser) {
            var scriptDict = new Dictionary<int, Dictionary<int, ScriptFunctionMetadata>>(); // scriptIds, functionDict
            foreach ((int scriptId, Parser.Xml.Table.Server.NpcScriptFunction scriptFunction) in scripts) {
                var presentItems = new List<ItemComponent>();
                for (int i = 0; i < scriptFunction.presentItemID.Length; i++) {
                    short itemRarity = scriptFunction.presentItemRank.ElementAtOrDefault(i) != default(short) ? scriptFunction.presentItemRank.ElementAtOrDefault(i) : (short) -1;
                    int itemAmount = scriptFunction.presentItemAmount.ElementAtOrDefault(i) != default ? scriptFunction.presentItemAmount.ElementAtOrDefault(i) : 1;
                    presentItems.Add(new ItemComponent(scriptFunction.presentItemID[i], itemRarity, itemAmount, ItemTag.None));
                }

                var collectItems = new List<ItemComponent>();
                for (int i = 0; i < scriptFunction.collectItemID.Length; i++) {
                    int itemAmount = scriptFunction.collectItemAmount.ElementAtOrDefault(i) != default ? scriptFunction.collectItemAmount.ElementAtOrDefault(i) : 1;
                    collectItems.Add(new ItemComponent(scriptFunction.collectItemID[i], -1, itemAmount, ItemTag.None));
                }

                var metadata = new ScriptFunctionMetadata(
                    Id: npcId, // NpcId or QuestId
                    ScriptId: scriptId,
                    Type: ScriptType.Npc,
                    FunctionId: scriptFunction.functionID,
                    EndFunction: scriptFunction.endFunction,
                    PortalId: scriptFunction.portal,
                    UiName: scriptFunction.uiName,
                    UiArg: scriptFunction.uiArg,
                    UiArg2: scriptFunction.uiArg2,
                    MoveMapId: scriptFunction.moveFieldID,
                    MovePortalId: scriptFunction.moveFieldPortalID,
                    MoveMapMovie: scriptFunction.moveFieldMovie,
                    Emoticon: scriptFunction.emoticon,
                    PresentItems: presentItems,
                    CollectItems: collectItems,
                    SetTriggerValueTriggerId: scriptFunction.setTriggerValueTriggerID,
                    SetTriggerValueKey: scriptFunction.setTriggerValueKey,
                    SetTriggerValue: scriptFunction.setTriggerValue,
                    Divorce: scriptFunction.divorce,
                    PresentExp: scriptFunction.presentExp,
                    CollectMeso: scriptFunction.collectMeso,
                    MaidMoodIncrease: scriptFunction.maidMoodUp,
                    MaidClosenessIncrease: scriptFunction.maidAffinityUp,
                    MaidPay: scriptFunction.maidPay
                );
                if (!scriptDict.TryGetValue(scriptId, out Dictionary<int, ScriptFunctionMetadata>? functionDict)) {
                    functionDict = new Dictionary<int, ScriptFunctionMetadata> {
                        {
                            scriptFunction.functionID, metadata
                        },
                    };
                    scriptDict.Add(scriptId, functionDict);
                } else {
                    functionDict.Add(scriptFunction.functionID, metadata);
                }
            }
            results.Add(npcId, scriptDict);
        }
        return results;
    }

    private static Dictionary<int, Dictionary<int, Dictionary<int, ScriptFunctionMetadata>>> MergeQuestScriptFunctions(Dictionary<int, Dictionary<int, Dictionary<int, ScriptFunctionMetadata>>> results, IEnumerable<(int NpcId, IDictionary<int, Parser.Xml.Table.Server.QuestScriptFunction> ScriptFunctions)> parser) {
        foreach ((int questId, IDictionary<int, Parser.Xml.Table.Server.QuestScriptFunction> scripts) in parser) {
            var scriptDict = new Dictionary<int, Dictionary<int, ScriptFunctionMetadata>>(); // scriptIds, functionDict
            foreach ((int scriptId, Parser.Xml.Table.Server.QuestScriptFunction scriptFunction) in scripts) {
                var presentItems = new List<ItemComponent>();
                for (int i = 0; i < scriptFunction.presentItemID.Length; i++) {
                    short itemRarity = scriptFunction.presentItemRank.ElementAtOrDefault(i) != default(short) ? scriptFunction.presentItemRank.ElementAtOrDefault(i) : (short) -1;
                    int itemAmount = scriptFunction.presentItemAmount.ElementAtOrDefault(i) != default ? scriptFunction.presentItemAmount.ElementAtOrDefault(i) : 1;
                    presentItems.Add(new ItemComponent(scriptFunction.presentItemID[i], itemRarity, itemAmount, ItemTag.None));
                }

                var collectItems = new List<ItemComponent>();
                for (int i = 0; i < scriptFunction.collectItemID.Length; i++) {
                    int itemAmount = scriptFunction.collectItemAmount.ElementAtOrDefault(i) != default ? scriptFunction.collectItemAmount.ElementAtOrDefault(i) : 1;
                    collectItems.Add(new ItemComponent(scriptFunction.collectItemID[i], -1, itemAmount, ItemTag.None));
                }

                var metadata = new ScriptFunctionMetadata(
                    Id: questId,
                    ScriptId: scriptId,
                    Type: ScriptType.Quest,
                    FunctionId: scriptFunction.functionID,
                    EndFunction: scriptFunction.endFunction,
                    PortalId: scriptFunction.portal,
                    UiName: scriptFunction.uiName,
                    UiArg: scriptFunction.uiArg,
                    UiArg2: scriptFunction.uiArg2,
                    MoveMapId: scriptFunction.moveFieldID,
                    MovePortalId: scriptFunction.moveFieldPortalID,
                    MoveMapMovie: scriptFunction.moveFieldMovie,
                    Emoticon: scriptFunction.emoticon,
                    PresentItems: presentItems,
                    CollectItems: collectItems,
                    SetTriggerValueTriggerId: scriptFunction.setTriggerValueTriggerID,
                    SetTriggerValueKey: scriptFunction.setTriggerValueKey,
                    SetTriggerValue: scriptFunction.setTriggerValue,
                    Divorce: scriptFunction.divorce,
                    PresentExp: scriptFunction.presentExp,
                    CollectMeso: scriptFunction.collectMeso,
                    MaidMoodIncrease: scriptFunction.maidMoodUp,
                    MaidClosenessIncrease: scriptFunction.maidAffinityUp,
                    MaidPay: scriptFunction.maidPay
                );
                if (!scriptDict.TryGetValue(scriptId, out Dictionary<int, ScriptFunctionMetadata>? functionDict)) {
                    functionDict = new Dictionary<int, ScriptFunctionMetadata> {
                        {
                            scriptFunction.functionID, metadata
                        },
                    };
                    scriptDict.Add(scriptId, functionDict);
                } else {
                    functionDict.Add(scriptFunction.functionID, metadata);
                }
            }
            results.Add(questId, scriptDict);
        }
        return results;
    }

    private JobConditionTable ParseJobCondition() {
        var results = new Dictionary<int, JobConditionMetadata>();
        foreach ((int npcId, Parser.Xml.Table.Server.JobConditionTable jobCondition) in parser.ParseJobConditionTable()) {
            DateTime date = DateTime.TryParseExact(jobCondition.date, "yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ? date : DateTime.MinValue;
            results.Add(npcId, new JobConditionMetadata(
                NpcId: npcId,
                ScriptId: jobCondition.scriptID,
                StartedQuestId: jobCondition.quest_start,
                CompletedQuestId: jobCondition.quest_complete,
                JobCode: (JobCode) jobCondition.job,
                MaidAuthority: jobCondition.maid_auth,
                MaidClosenessTime: jobCondition.maid_affinity_time,
                MaidCosenessRank: jobCondition.maid_affinity_grade,
                Date: date.ToEpochSeconds(),
                BuffId: jobCondition.buff,
                Mesos: jobCondition.meso,
                Level: jobCondition.level,
                Home: jobCondition.home,
                Roulette: jobCondition.roulette,
                Guild: jobCondition.guild,
                CompletedAchievement: jobCondition.achieve_complete,
                IsBirthday: jobCondition.birthday,
                ChangeToJobCode: (JobCode) jobCondition.jobCode,
                MapId: jobCondition.map,
                MoveMapId: jobCondition.moveFieldID,
                MovePortalId: jobCondition.movePortalID
            ));
        }

        return new JobConditionTable(results);
    }

    private BonusGameTable ParseBonusGameTable() {
        var bonusGames = new Dictionary<int, BonusGameTable.Game>();
        foreach ((int type, int id, BonusGame bonusGame) in parser.ParseBonusGame()) {
            List<BonusGameTable.Game.Slot> slots = [];
            foreach (BonusGame.Slot slot in bonusGame.slot) {
                slots.Add(new BonusGameTable.Game.Slot(
                    MinProp: slot.minProp,
                    MaxProp: slot.maxProp));
            }
            bonusGames.Add(id, new BonusGameTable.Game(
                Id: id,
                ConsumeItem: new ItemComponent(
                    ItemId: bonusGame.consumeItemID,
                    Rarity: -1,
                    Amount: bonusGame.consumeItemCount,
                    Tag: ItemTag.None),
                Slots: slots.ToArray()));
        }

        var drops = new Dictionary<int, BonusGameTable.Drop>();
        foreach ((int type, int id, BonusGameDrop gameDrop) in parser.ParseBonusGameDrop()) {
            List<BonusGameTable.Drop.Item> items = [];
            foreach (BonusGameDrop.Item item in gameDrop.item) {
                items.Add(new BonusGameTable.Drop.Item(
                    ItemComponent: new ItemComponent(
                        ItemId: item.id,
                        Rarity: item.rank,
                        Amount: item.count,
                        Tag: ItemTag.None),
                    Probability: item.prop,
                    Notice: item.notice));
            }
            drops.Add(id, new BonusGameTable.Drop(
                Id: id,
                Items: items.ToArray()));
        }

        return new BonusGameTable(bonusGames, drops);
    }

    private GlobalDropItemBoxTable ParseGlobalItemDropTable() {
        var dropGroups = new Dictionary<int, Dictionary<int, IList<GlobalDropItemBoxTable.Group>>>();

        foreach ((int id, GlobalDropItemBox itemDrop) in parser.ParseGlobalDropItemBox()) {
            var groups = new List<GlobalDropItemBoxTable.Group>();
            foreach (GlobalDropItemBox.Group group in itemDrop.v) {
                List<GlobalDropItemBoxTable.Group.DropCount> dropCounts = [];
                for (int i = 0; i < group.dropCount.Length; i++) {
                    dropCounts.Add(new GlobalDropItemBoxTable.Group.DropCount(
                        Amount: group.dropCount[i],
                        Probability: group.dropCountProbability[i]));
                }
                groups.Add(new GlobalDropItemBoxTable.Group(
                    GroupId: group.dropGroupIDs,
                    MinLevel: group.minLevel,
                    MaxLevel: group.maxLevel,
                    DropCounts: dropCounts,
                    OwnerDrop: group.isOwnerDrop,
                    MapTypeCondition: (MapType) group.mapTypeCondition,
                    ContinentCondition: (Continent) group.continentCondition));
            }

            if (!dropGroups.TryGetValue(id, out Dictionary<int, IList<GlobalDropItemBoxTable.Group>>? groupDict)) {
                groupDict = new Dictionary<int, IList<GlobalDropItemBoxTable.Group>> {
                    { id, groups }
                };
                dropGroups.Add(id, groupDict);
            } else {
                groupDict.Add(id, groups);
            }
        }

        var dropItems = new Dictionary<int, IList<GlobalDropItemBoxTable.Item>>();
        foreach ((int id, GlobalDropItemSet itemBox) in parser.ParseGlobalDropItemSet()) {
            var items = new List<GlobalDropItemBoxTable.Item>();

            foreach (GlobalDropItemSet.Item item in itemBox.v) {
                int minCount = item.minCount <= 0 ? 1 : item.minCount;
                int maxCount = item.maxCount < item.minCount ? item.minCount : item.maxCount;
                items.Add(new GlobalDropItemBoxTable.Item(
                    Id: item.itemID,
                    MinLevel: item.minLevel,
                    MaxLevel: item.maxLevel,
                    DropCount: new GlobalDropItemBoxTable.Range<int>(minCount, maxCount),
                    Rarity: item.grade,
                    Weight: item.weight,
                    MapIds: item.mapDependency,
                    QuestConstraint: item.constraintsQuest));
            }

            dropItems.Add(id, items);
        }
        return new GlobalDropItemBoxTable(dropGroups, dropItems);
    }

    private UserStatTable ParseUserStat() {
        static IReadOnlyDictionary<BasicAttribute, long> UserStatMetadataMapper(UserStat userStat) {
            Dictionary<BasicAttribute, long> stats = new() {
                { BasicAttribute.Strength, (long) userStat.str },
                { BasicAttribute.Dexterity, (long) userStat.dex },
                { BasicAttribute.Intelligence, (long) userStat.@int },
                { BasicAttribute.Luck, (long) userStat.luk },
                { BasicAttribute.Health, (long) userStat.hp },
                { BasicAttribute.HpRegen, (long) userStat.hp_rgp },
                { BasicAttribute.HpRegenInterval, (long) userStat.hp_inv },
                { BasicAttribute.Spirit, (long) userStat.sp },
                { BasicAttribute.SpRegen, (long) userStat.sp_rgp },
                { BasicAttribute.SpRegenInterval, (long) userStat.sp_inv },
                { BasicAttribute.Stamina, (long) userStat.ep },
                { BasicAttribute.StaminaRegen, (long) userStat.ep_rgp },
                { BasicAttribute.StaminaRegenInterval, (long) userStat.ep_inv },
                { BasicAttribute.AttackSpeed, (long) userStat.asp },
                { BasicAttribute.MovementSpeed, (long) userStat.msp },
                { BasicAttribute.Accuracy, (long) userStat.atp },
                { BasicAttribute.Evasion, (long) userStat.evp },
                { BasicAttribute.CriticalRate, (long) userStat.cap },
                { BasicAttribute.CriticalDamage, (long) userStat.cad },
                { BasicAttribute.CriticalEvasion, (long) userStat.car },
                { BasicAttribute.Defense, (long) userStat.ndd },
                { BasicAttribute.PerfectGuard, (long) userStat.abp },
                { BasicAttribute.JumpHeight, (long) userStat.jmp },
                { BasicAttribute.PhysicalAtk, (long) userStat.pap },
                { BasicAttribute.MagicalAtk, (long) userStat.map },
                { BasicAttribute.PhysicalRes, (long) userStat.par },
                { BasicAttribute.MagicalRes, (long) userStat.mar },
                { BasicAttribute.MinWeaponAtk, (long) userStat.wapmin },
                { BasicAttribute.MaxWeaponAtk, (long) userStat.wapmax },
                { BasicAttribute.Damage, (long) userStat.dmg },
                { BasicAttribute.Piercing, (long) userStat.pen },
                { BasicAttribute.BonusAtk, (long) userStat.base_atk },
                { BasicAttribute.PetBonusAtk, (long) userStat.sp_value }
            };

            return stats;
        }

        return new UserStatTable(
            new Dictionary<JobCode, IReadOnlyDictionary<short, IReadOnlyDictionary<BasicAttribute, long>>> {
                { JobCode.Newbie, parser.ParseUserStat1().ToDictionary(x => x.Level, x => UserStatMetadataMapper(x.UserStat)) },
                { JobCode.Knight, parser.ParseUserStat10().ToDictionary(x => x.Level, x => UserStatMetadataMapper(x.UserStat)) },
                { JobCode.Berserker, parser.ParseUserStat20().ToDictionary(x => x.Level, x => UserStatMetadataMapper(x.UserStat)) },
                { JobCode.Wizard, parser.ParseUserStat30().ToDictionary(x => x.Level, x => UserStatMetadataMapper(x.UserStat)) },
                { JobCode.Priest, parser.ParseUserStat40().ToDictionary(x => x.Level, x => UserStatMetadataMapper(x.UserStat)) },
                { JobCode.Archer, parser.ParseUserStat50().ToDictionary(x => x.Level, x => UserStatMetadataMapper(x.UserStat)) },
                { JobCode.HeavyGunner, parser.ParseUserStat60().ToDictionary(x => x.Level, x => UserStatMetadataMapper(x.UserStat)) },
                { JobCode.Thief, parser.ParseUserStat70().ToDictionary(x => x.Level, x => UserStatMetadataMapper(x.UserStat)) },
                { JobCode.Assassin, parser.ParseUserStat80().ToDictionary(x => x.Level, x => UserStatMetadataMapper(x.UserStat)) },
                { JobCode.RuneBlader, parser.ParseUserStat90().ToDictionary(x => x.Level, x => UserStatMetadataMapper(x.UserStat)) },
                { JobCode.Striker, parser.ParseUserStat100().ToDictionary(x => x.Level, x => UserStatMetadataMapper(x.UserStat)) },
                { JobCode.SoulBinder, parser.ParseUserStat110().ToDictionary(x => x.Level, x => UserStatMetadataMapper(x.UserStat)) }
            }
        );
    }

    private IndividualDropItemTable ParseIndividualItemDropTable() {
        var results = new Dictionary<int, IDictionary<int, IndividualDropItemTable.Entry>>();

        foreach ((int id, IndividualItemDrop dropBox) in parser.ParseIndividualItemDrop()) {
            var entries = new Dictionary<int, IndividualDropItemTable.Entry>();

            foreach (IndividualItemDrop.Group group in dropBox.group) {
                List<IndividualDropItemTable.Item> items = new();
                foreach (IndividualItemDrop.Group.Item item in group.v) {
                    int minCount = item.minCount <= 0 ? 1 : item.minCount;
                    int maxCount = item.maxCount < item.minCount ? item.minCount : item.maxCount;
                    List<IndividualDropItemTable.Item.Rarity> rarities = item.gradeProbability
                        .Select((probability, i) => new IndividualDropItemTable.Item.Rarity(probability, item.grade[i]))
                        .ToList();

                    if (rarities.Count == 0) {
                        if (item.grade.Length > 0) {
                            foreach (short grade in item.grade) {
                                rarities.Add(new IndividualDropItemTable.Item.Rarity(100, grade));
                            }
                        } else if (item.uiItemRank != 0) {
                            rarities.Add(new IndividualDropItemTable.Item.Rarity(100, item.uiItemRank));
                        }
                    }
                    items.Add(new IndividualDropItemTable.Item(
                        Ids: [item.itemID, item.itemID2],
                        Announce: item.isAnnounce,
                        ProperJobWeight: item.properJobWeight,
                        ImproperJobWeight: item.imProperJobWeight,
                        Weight: item.weight,
                        DropCount: new IndividualDropItemTable.Range<int>(minCount, maxCount),
                        Rarities: rarities,
                        EnchantLevel: item.enchantLevel,
                        SocketDataId: item.socketDataID,
                        DeductTradeCount: item.tradableCountDeduction,
                        DeductRepackLimit: item.rePackingLimitCountDeduction,
                        Bind: item.isBindCharacter,
                        DisableBreak: item.disableBreak,
                        MapIds: item.mapDependency,
                        QuestId: item.constraintsQuest ? GetQuestId(dropBox.comment, item.reference1) : 0
                    ));
                }

                IList<IndividualDropItemTable.Entry.DropCount> dropCounts = group.dropCount.Zip(group.dropCountProbability, (count, probability) => new IndividualDropItemTable.Entry.DropCount(count, probability)).ToList();
                if (dropCounts.Count == 0) {
                    dropCounts.Add(new IndividualDropItemTable.Entry.DropCount(1, 100));
                }

                var entry = new IndividualDropItemTable.Entry(
                    GroupId: group.dropGroupID,
                    SmartDropRate: group.smartDropRate,
                    DropCounts: dropCounts,
                    MinLevel: group.dropGroupMinLevel,
                    ServerDrop: group.serverDrop,
                    SmartGender: group.isApplySmartGenderDrop,
                    Items: items
                );

                entries.Add(group.dropGroupID, entry);

            }

            results.Add(id, entries);
        }
        return new IndividualDropItemTable(results);

        int GetQuestId(string comment, string reference1) {
            if (reference1.Contains("Quest")) {
                string[] referenceArray = reference1.Split("/");
                int referenceQuestIndex = Array.IndexOf(referenceArray, "Quest");

                if (!int.TryParse(referenceArray[referenceQuestIndex - 2], out int questId) && comment.Contains("Quest")) {
                    string[] commentArray = comment.Split("/");
                    int commentQuestIndex = Array.IndexOf(commentArray, "Quest");
                    if (string.IsNullOrEmpty(commentArray[commentQuestIndex - 2])) {
                        return 0;
                    }
                    return !int.TryParse(commentArray[commentQuestIndex - 2], out questId) ? 0 : questId;
                }
            }
            return 0;
        }
    }

    private PrestigeExpTable ParsePrestigeExpTable() {
        var results = new Dictionary<ExpType, long>();

        foreach ((AdventureExpType type, AdventureExpTable table) in parser.ParseAdventureExp()) {
            ExpType expType = ToExpType(type);
            results.Add(expType, table.value);
        }

        return new PrestigeExpTable(results);
    }

    private static ExpType ToExpType(AdventureExpType type) {
        return type switch {
            AdventureExpType.Exp_MapCommon => ExpType.mapCommon,
            AdventureExpType.Exp_MapHidden => ExpType.mapHidden,
            AdventureExpType.Exp_TaxiStation => ExpType.taxi,
            AdventureExpType.Exp_Telescope => ExpType.telescope,
            AdventureExpType.Exp_RareChest => ExpType.rareChest,
            AdventureExpType.Exp_RareChestFirst => ExpType.rareChestFirst,
            AdventureExpType.Exp_NormalChest => ExpType.normalChest,
            AdventureExpType.Exp_DropItem => ExpType.dropItem,
            AdventureExpType.Exp_DungeonBoss => ExpType.dungeonBoss,
            AdventureExpType.Exp_MusicMasteryLv1 => ExpType.musicMastery1,
            AdventureExpType.Exp_MusicMasteryLv2 => ExpType.musicMastery2,
            AdventureExpType.Exp_MusicMasteryLv3 => ExpType.musicMastery3,
            AdventureExpType.Exp_MusicMasteryLv4 => ExpType.musicMastery4,
            AdventureExpType.Exp_Arcade => ExpType.arcade,
            AdventureExpType.Exp_Fishing => ExpType.fishing,
            AdventureExpType.Exp_Rest => ExpType.rest,
            AdventureExpType.Exp_Quest => ExpType.quest,
            AdventureExpType.Exp_PvpBloodMineRank1 => ExpType.bloodMineRank1,
            AdventureExpType.Exp_PvpBloodMineRank2 => ExpType.bloodMineRank2,
            AdventureExpType.Exp_PvpBloodMineRank3 => ExpType.bloodMineRank3,
            AdventureExpType.Exp_PvpBloodMineRankOther => ExpType.bloodMineRankOther,
            AdventureExpType.Exp_PvpRedDuelWin => ExpType.redDuelWin,
            AdventureExpType.Exp_PvpRedDuelLose => ExpType.redDuelLose,
            AdventureExpType.Exp_PvpBtiTeamWin => ExpType.btiTeamWin,
            AdventureExpType.Exp_PvpBtiTeamLose => ExpType.btiTeamLose,
            AdventureExpType.Exp_PvpRankDuelWin => ExpType.rankDuelWin,
            AdventureExpType.Exp_PvpRankDuelLose => ExpType.rankDuelLose,
            AdventureExpType.Exp_Gathering => ExpType.gathering,
            AdventureExpType.Exp_Manufacturing => ExpType.manufacturing,
            AdventureExpType.Exp_RandomDungeonBonus => ExpType.randomDungeonBonus,
            AdventureExpType.Exp_MiniGame => ExpType.miniGame,
            AdventureExpType.Exp_UserMiniGame => ExpType.userMiniGame,
            AdventureExpType.Exp_UserMiniGameExtra => ExpType.userMiniGameExtra,
            AdventureExpType.Exp_Mission => ExpType.mission,
            AdventureExpType.Exp_DungeonRelative => ExpType.dungeonRelative,
            AdventureExpType.Exp_GuildUserExp => ExpType.guildUserExp,
            AdventureExpType.Exp_DailyGuildQuest => ExpType.dailyGuildQuest,
            AdventureExpType.Exp_WeeklyGuildQuest => ExpType.weeklyGuildQuest,
            AdventureExpType.Exp_PetTaming => ExpType.petTaming,
            AdventureExpType.Exp_DailyMission => ExpType.dailymission,
            AdventureExpType.Exp_DailyMissionLevelUp => ExpType.dailymissionLevelUp,
            AdventureExpType.Exp_mapleSurvival => ExpType.mapleSurvival,
            AdventureExpType.Exp_DarkStream => ExpType.darkStream,
            AdventureExpType.Exp_DungeonClear => ExpType.dungeonClear,
            AdventureExpType.Exp_KillMonster => ExpType.monster,
            AdventureExpType.Exp_QuestETC => ExpType.questEtc,
            AdventureExpType.Exp_EpicQuest => ExpType.epicQuest,
            AdventureExpType.Exp_KillMonsterBoss => ExpType.monsterBoss,
            AdventureExpType.Exp_KillMonsterElite => ExpType.monsterElite,
            _ => ExpType.none,
        };
    }
}

