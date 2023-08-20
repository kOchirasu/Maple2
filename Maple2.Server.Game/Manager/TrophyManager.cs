using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;
using Serilog;

namespace Maple2.Server.Game.Manager;

public sealed class TrophyManager {
    private const int BATCH_SIZE = 60;
    private readonly GameSession session;

    public IDictionary<int, TrophyEntry> Values { get; }

    private readonly ILogger logger = Log.Logger.ForContext<TrophyManager>();

    public TrophyManager(GameSession session) {
        this.session = session;

        using GameStorage.Request db = session.GameStorage.Context();
        Values = db.GetAccountTrophy(session.AccountId);
    }

    public void Load() {
        session.Send(TrophyPacket.Initialize());
        foreach (ImmutableList<TrophyEntry> batch in Values.Values.Batch(BATCH_SIZE)) {
            session.Send(TrophyPacket.Load(batch));
        }
    }


    /// <summary>
    /// Checks for any possible trophies under stated TrophyConditionType. If there is a trophy that can have any progress updated to it, update or add to player if it hasn't existed yet.
    /// </summary>
    /// <param name="conditionType">TrophyConditionType to search metadata</param>
    /// <param name="count">Condition value to update progress of a trophy. Default is 1.</param>
    /// <param name="targetString">Trophy grade condition target parameter in string.</param>
    /// <param name="targetLong">Trophy grade condition target parameter in long.</param>
    /// <param name="codeString">Trophy grade condition code parameter in string.</param>
    /// <param name="codeLong">Trophy grade condition code parameter in long.</param>
    public void Update(TrophyConditionType conditionType, long count = 1, string targetString = "", long targetLong = 0, string codeString = "", long codeLong = 0) {
        IEnumerable<TrophyMetadata> metadatas = session.TrophyMetadata.GetMany(conditionType);

        foreach (TrophyMetadata metadata in metadatas) {
            if (!Values.TryGetValue(metadata.Id, out TrophyEntry? trophy) || !metadata.Grades.TryGetValue(trophy.CurrentGrade, out TrophyMetadataGrade? grade)) {
                grade = metadata.Grades[1];
            }

            if (grade.Condition.Codes != null && !CheckCode(grade.Condition, codeString, codeLong)) {
                continue;
            }

            if (grade.Condition.Target != null && !CheckTarget(grade.Condition)) {
                continue;
            }

            if (trophy == null) {
                trophy = new TrophyEntry(metadata) {
                    CurrentGrade = 1,
                    RewardGrade = 1,
                };
                Values.Add(metadata.Id, trophy);
            }

            if (!RankUp(trophy, count)) {
                session.Send(TrophyPacket.Update(trophy));
            }
        }
    }

    private bool CheckCode(TrophyMetadataCondition condition, string stringValue = "", long longValue = 0) {
        TrophyMetadataCondition.Code code = condition.Codes!;
        switch (condition.Type) {
            case TrophyConditionType.map:
                if (code.Range != null && InRange((TrophyMetadataCondition.Range<int>) code.Range, session.Player.Value.Character.MapId)) {
                    return true;
                }

                if (code.Integers != null && code.Integers.Contains(session.Player.Value.Character.MapId)) {
                    return true;
                }
                break;
            case TrophyConditionType.jump:
            case TrophyConditionType.meso:
            case TrophyConditionType.taxifind:
            case TrophyConditionType.fall_damage:
                return true;
            case TrophyConditionType.emotion:
                if (code.Strings != null && code.Strings.Contains(stringValue)) {
                    return true;
                }
                break;
            case TrophyConditionType.trophy_point:
                if (code.Range != null && InRange((TrophyMetadataCondition.Range<int>) code.Range, longValue)) {
                    return true;
                }
                break;
            case TrophyConditionType.interact_object:
                if ((code.Range != null && InRange((TrophyMetadataCondition.Range<int>) code.Range, longValue)) ||
                    (code.Integers != null && code.Integers.Contains((int) longValue))) {
                    if (session.Player.Value.Unlock.InteractedObjects.Contains((int) longValue)) {
                        return false;
                    }
                    session.Player.Value.Unlock.InteractedObjects.Add((int) longValue);
                    return true;
                }
                break;
            case TrophyConditionType.item_collect:
            case TrophyConditionType.item_collect_revise:
                if ((code.Range != null && InRange((TrophyMetadataCondition.Range<int>) code.Range, longValue)) ||
                    (code.Integers != null && code.Integers.Contains((int) longValue))) {
                    if (session.Player.Value.Unlock.ItemCollects.ContainsKey((int) longValue)) {
                        session.Player.Value.Unlock.ItemCollects[(int) longValue]++;
                        return false;
                    }
                    
                    session.Player.Value.Unlock.ItemCollects.Add((int) longValue, 1);
                    return true;
                }
                break;

        }
        return false;

        bool InRange(TrophyMetadataCondition.Range<int> range, long value) {
            return value >= range.Min && value <= range.Max;
        }
    }

    private bool CheckTarget(TrophyMetadataCondition condition, long longValue = 0) {
        TrophyMetadataCondition.Code target = condition.Target!;
        switch (condition.Type) {
            case TrophyConditionType.map:
            case TrophyConditionType.jump:
            case TrophyConditionType.meso:
            case TrophyConditionType.taxifind:
            case TrophyConditionType.trophy_point:
            case TrophyConditionType.interact_object:
                return true;
            case TrophyConditionType.emotion:
                if (target.Range != null && target.Range.Value.Min >= session.Player.Value.Character.MapId &&
                    target.Range.Value.Max <= session.Player.Value.Character.MapId) {
                    return true;
                }
                break;
            case TrophyConditionType.fall_damage:
                if (target.Range != null && target.Range.Value.Min >= longValue &&
                    target.Range.Value.Max <= longValue) {
                    return true;
                }

                if (target.Integers != null && target.Integers.Any(value => value >= longValue)) {
                    return true;
                }
                break;
        }
        return false;
    }

    /// <summary>
    /// Checks if trophy has reached a new grade. Provides rewards only on certain reward types.
    /// </summary>
    /// <param name="trophy">Trophy entry from player</param>
    /// <param name="count">Count amount to increment on for the trophy.</param>
    /// <returns>False if there is no rank up possible or condition value has not been met.</returns>
    private bool RankUp(TrophyEntry trophy, long count = 1) {
        trophy.Counter += count;

        if (!trophy.Metadata.Grades.TryGetValue(trophy.CurrentGrade, out TrophyMetadataGrade? grade)) {
            return false;
        }

        int newGradesCount = 0;
        while (trophy.Counter >= grade.Condition.Value) {
            if (trophy.Completed) {
                break;
            }
            trophy.Grades.Add(trophy.CurrentGrade, DateTime.Now.ToEpochSeconds());
            newGradesCount++;

            if (trophy.Grades.Count < trophy.Metadata.Grades.Count) {
                trophy.CurrentGrade++;
                Reward(trophy.Id);
            }

            // Update count on player
            switch (trophy.Category) {
                case TrophyCategory.Combat:
                    session.Player.Value.Account.Trophy.Combat++;
                    break;
                case TrophyCategory.Adventure:
                    session.Player.Value.Account.Trophy.Adventure++;
                    break;
                case TrophyCategory.Life:
                    session.Player.Value.Account.Trophy.Lifestyle++;
                    break;
            }

            session.Send(TrophyPacket.Update(trophy));
            if (!trophy.Metadata.Grades.TryGetValue(trophy.CurrentGrade, out grade)) {
                break;
            }
        }

        if (newGradesCount > 0) {
            Update(TrophyConditionType.trophy_point, newGradesCount, codeLong: trophy.Id);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gives rewards (if applicable) from awarded trophy grades. If there is no reward, it'll increment up on the reward grade.
    /// </summary>
    /// <param name="trophyId">Trophy Id</param>
    /// <param name="manualClaim">If true, assumes player requested the reward. it will give the player rewards for items, titles, skill points, and attribute points.
    /// These are never given automatically upon newly awarded trophy grade.</param>
    public void Reward(int trophyId, bool manualClaim = false) {
        if (!Values.TryGetValue(trophyId, out TrophyEntry? trophy)) {
            return;
        }

        if (trophy.CurrentGrade < trophy.RewardGrade) {
            return;
        }

        for (int startGrade = trophy.RewardGrade; startGrade <= trophy.CurrentGrade; startGrade++) {
            if (!trophy.Metadata.Grades.TryGetValue(startGrade, out TrophyMetadataGrade? grade)) {
                continue;
            }

            if (grade.Reward == null) {
                trophy.RewardGrade++;
                continue;
            }

            if (trophy.RewardGrade == trophy.CurrentGrade && !trophy.Completed) {
                session.Send(TrophyPacket.Update(trophy));
                break;
            }

            switch (grade.Reward.Type) {
                case TrophyRewardType.item:
                    if (!manualClaim) {
                        return;
                    }
                    Item? item = session.Item.CreateItem(grade.Reward.Code, grade.Reward.Rank, grade.Reward.Value);
                    if (item == null) {
                        continue;
                    }
                    if (!session.Item.Inventory.Add(item, true)) {
                        session.Item.MailItem(item);
                    }
                    break;
                case TrophyRewardType.title:
                    if (!manualClaim) {
                        return;
                    }

                    if (session.Player.Value.Unlock.Titles.Contains(grade.Reward.Code)) {
                        break;
                    }
                    session.Send(UserEnvPacket.AddTitle(grade.Reward.Code));
                    session.Player.Value.Unlock.Titles.Add(grade.Reward.Code);
                    break;
                case TrophyRewardType.dynamicaction:
                    if (session.Player.Value.Unlock.Emotes.Contains(grade.Reward.Code)) {
                        break;
                    }
                    session.Player.Value.Unlock.Emotes.Add(grade.Reward.Code);
                    session.Send(EmotePacket.Learn(new Emote(grade.Reward.Code)));
                    break;
                case TrophyRewardType.beauty_hair:
                case TrophyRewardType.beauty_makeup:
                case TrophyRewardType.beauty_skin:
                case TrophyRewardType.itemcoloring:
                case TrophyRewardType.shop_build:
                case TrophyRewardType.shop_ride:
                case TrophyRewardType.shop_weapon:
                case TrophyRewardType.etc: // currently used as quest unlocks
                    // I don't think anything is supposed to happen here. Just client sided visuals?
                    break;
                default:
                    logger.Error("Unimplemented trophy reward type {RewardType}", grade.Reward.Type);
                    break;
            }

            trophy.RewardGrade++;
            session.Send(TrophyPacket.Update(trophy));
        }
    }
}
