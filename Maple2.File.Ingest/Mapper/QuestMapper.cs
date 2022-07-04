using M2dXmlGenerator;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Quest;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class QuestMapper : TypeMapper<QuestMetadata> {
    private readonly QuestParser parser;

    public QuestMapper(M2dReader xmlReader) {
        parser = new QuestParser(xmlReader);
    }

    protected override IEnumerable<QuestMetadata> Map() {
        foreach ((int id, string name, QuestData data) in parser.Parse()) {
            if (data.start == null || data.complete == null) {
                continue;
            }

            yield return new QuestMetadata(
                Id: id,
                Name: name,
                Basic: new QuestMetadataBasic(
                    ChapterId: data.basic.chapterID,
                    Type: data.basic.questType,
                    Account: data.basic.account,
                    StandardLevel: data.basic.standardLevel,
                    AutoStart: data.basic.autoStart,
                    StartNpc: data.start.npc,
                    CompleteNpc: data.complete.npc,
                    CompleteMap: data.complete.map
                ),
                Require: new QuestMetadataRequire(
                    Level: data.require.level,
                    MaxLevel: data.require.maxLevel,
                    Job: data.require.job,
                    Quest: data.require.quest,
                    SelectableQuest: data.require.selectableQuest,
                    Achievement: data.require.achievement,
                    GearScore: data.require.gearScore
                ),
                AcceptReward: Convert(data.acceptReward),
                CompleteReward: Convert(data.completeReward)
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
}
