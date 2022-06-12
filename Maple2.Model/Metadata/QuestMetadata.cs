using System.Collections.Generic;

namespace Maple2.Model.Metadata;

public record QuestMetadata(
    int Id,
    string? Name,
    QuestMetadataBasic Basic,
    QuestMetadataRequire Require,
    QuestMetadataReward AcceptReward,
    QuestMetadataReward CompleteReward);

public record QuestMetadataBasic(
    int ChapterId,
    int Type,
    int Account,
    int StandardLevel,
    bool AutoStart,
    int StartNpc,
    int CompleteNpc,
    int[] CompleteMap);

public record QuestMetadataRequire(
    short Level,
    short MaxLevel,
    int[] Job,
    int[] Quest,
    int[] SelectableQuest,
    int Achievement,
    int GearScore);

public record QuestMetadataReward(
    int Meso,
    int Exp,
    int GuildFund,
    int GuildExp,
    int GuildCoin,
    int MenteeCoin,
    int MissionPoint,
    int Treva,
    int Rue,
    List<QuestMetadataReward.Item> EssentialItem,
    List<QuestMetadataReward.Item> EssentialJobItem) {

    public record Item(int Id, int Rarity, int Amount);
}
