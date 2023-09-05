using System.Diagnostics;
using M2dXmlGenerator;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Quest;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class QuestMapper : TypeMapper<QuestMetadata> {
    private readonly QuestParser parser;

    public QuestMapper(M2dReader xmlReader) {
        parser = new QuestParser(xmlReader);
    }

    protected override IEnumerable<QuestMetadata> Map() {
        foreach ((int id, string name, QuestData data) in parser.Parse()) {
            Debug.Assert(Enum.IsDefined((QuestType) data.basic.questType), $"Invalid QuestType: {data.basic.questType}");
            var unrequiredAchievement = (0, 0);
            if (data.require.unreqAchievement.Length == 2 &&
                int.TryParse(data.require.unreqAchievement[0], out int achievementId) &&
                int.TryParse(data.require.unreqAchievement[1], out int grade)) {
                unrequiredAchievement = (achievementId, grade);
            }
            yield return new QuestMetadata(
                Id: id,
                Name: name,
                Basic: new QuestMetadataBasic(
                    ChapterId: data.basic.chapterID,
                    Type: (QuestType) data.basic.questType,
                    Account: data.basic.account,
                    StandardLevel: data.basic.standardLevel,
                    Forfeitable: !data.basic.disableGiveup,
                    EventTag: data.basic.eventTag,
                    AutoStart: data.basic.autoStart,
                    Disabled: data.basic.locking,
                    StartNpc: data.start?.npc ?? 0,
                    CompleteNpc: data.complete?.npc ?? 0,
                    CompleteMaps: data.complete?.map,
                    ProgressMaps: data.progressMap.progressMap
                ),
                Require: new QuestMetadataRequire(
                    Level: data.require.level,
                    MaxLevel: data.require.maxLevel,
                    Job: data.require.job.Select(job => (JobCode) job).ToArray(),
                    Quest: data.require.quest,
                    SelectableQuest: data.require.selectableQuest,
                    Achievement: data.require.achievement,
                    UnrequiredAchievement: unrequiredAchievement,
                    GearScore: data.require.gearScore
                ),
                AcceptReward: Convert(data.acceptReward),
                CompleteReward: Convert(data.completeReward),
                GoToNpc: new QuestMetadataGoToNpc(
                    Enabled: data.gotoNpc.enable,
                    MapId: data.gotoNpc.gotoField,
                    PortalId: data.gotoNpc.gotoPortal),
                GoToDungeon: new QuestMetadataGoToDungeon(
                    State: (QuestState) data.gotoDungeon.state,
                    MapId: data.gotoDungeon.gotoDungeon,
                    InstanceId: data.gotoDungeon.gotoInstanceID),
                Conditions: data.condition.Select(condition => new QuestMetadataCondition(
                    Type: (QuestConditionType) condition.type,
                    Codes: GetParameters(condition.code),
                    Target: GetParameters(condition.target),
                    Value: condition.value,
                    PartyCount: condition.partyCount,
                    GuildPartyCount: condition.guildPartyCount
                )).ToArray()
            );
        }
    }

    private QuestMetadataReward Convert(Reward reward) {
        List<Reward.Item> essentialItem = reward.essentialItem;
        List<Reward.Item> essentialJobItem = reward.essentialJobItem;
        if (FeatureLocaleFilter.FeatureEnabled("GlobalQuestRewardItem")) {
            essentialItem = reward.globalEssentialItem.Count > 0 ? reward.globalEssentialItem : essentialItem;
            essentialJobItem = reward.globalEssentialJobItem.Count > 0 ? reward.globalEssentialJobItem : essentialJobItem;
        }

        return new QuestMetadataReward(
            Meso: reward.money,
            Exp: reward.exp,
            RelativeExp: (Model.Enum.ExpType) reward.relativeExp,
            GuildFund: reward.guildFund,
            GuildExp: reward.guildExp,
            GuildCoin: reward.guildCoin,
            Treva: reward.karma,
            Rue: reward.lu,
            MenteeCoin: reward.menteeCoin,
            MissionPoint: reward.missionPoint,
            EssentialItem: essentialItem.Select(item =>
                new QuestMetadataReward.Item(item.code, item.rank, item.count)).ToList(),
            EssentialJobItem: essentialJobItem.Select(item =>
                new QuestMetadataReward.Item(item.code, item.rank, item.count)).ToList()
        );
    }
    
    private QuestMetadataCondition.Parameters? GetParameters(string[] array) {
        if (array.Length == 0) {
            return null;
        }
        if (array.Length > 1) {
            List<int> integers = new();
            List<string> strings = new();
            foreach (string code in array) {
                if (!int.TryParse(code, out int intCode)) {
                    strings.Add(code);
                } else {
                    integers.Add(intCode);
                }
            }

            return new QuestMetadataCondition.Parameters(
                Strings: strings.Count == 0 ? null : strings.ToArray(),
                Integers: integers.Count == 0 ? null : integers.ToArray(),
                Range: null);
        }

        string[] split = array[0].Split('-');
        if (split.Length > 1) {
            return new QuestMetadataCondition.Parameters(
                Range: new QuestMetadataCondition.Range<int>(int.Parse(split[0]), int.Parse(split[1])));
        }

        if (!int.TryParse(array[0], out int integerResult)) {
            return new QuestMetadataCondition.Parameters(
                Strings: new[] {array[0]});
        }
        return new QuestMetadataCondition.Parameters(
            Integers: new[] {integerResult});
    }
}
