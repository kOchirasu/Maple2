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

public sealed class AchievementManager {
    private const int BATCH_SIZE = 60;
    private readonly GameSession session;

    public IDictionary<int, Achievement> Values => session.Player.Value.Character.Achievements;

    private readonly ILogger logger = Log.Logger.ForContext<AchievementManager>();

    public AchievementManager(GameSession session) {
        this.session = session;
    }

    public void Load() {
        session.Send(AchievementPacket.Initialize());
        foreach (ImmutableList<Achievement> batch in Values.Values.Batch(BATCH_SIZE)) {
            session.Send(AchievementPacket.Load(batch));
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
    public void Update(AchievementConditionType conditionType, long count = 1, string targetString = "", long targetLong = 0, string codeString = "", long codeLong = 0) {
        IEnumerable<AchievementMetadata> metadatas = session.AchievementMetadata.GetMany(conditionType);

        foreach (AchievementMetadata metadata in metadatas) {
            if (!Values.TryGetValue(metadata.Id, out Achievement? achievement) || !metadata.Grades.TryGetValue(achievement.CurrentGrade, out AchievementMetadataGrade? grade)) {
                grade = metadata.Grades[1];
            }

            if (grade.Condition.Codes != null && !CheckCode(grade.Condition, codeString, codeLong)) {
                continue;
            }

            if (grade.Condition.Target != null && !CheckTarget(grade.Condition)) {
                continue;
            }

            if (achievement == null) {
                achievement = new Achievement(metadata) {
                    CurrentGrade = 1,
                    RewardGrade = 1,
                };
                Values.Add(metadata.Id, achievement);
            }

            if (!RankUp(achievement, count)) {
                session.Send(AchievementPacket.Update(achievement));
            }
        }
    }

    private bool CheckCode(AchievementMetadataCondition condition, string stringValue = "", long longValue = 0) {
        AchievementMetadataCondition.Parameters parameters = condition.Codes!;
        switch (condition.Type) {
            case AchievementConditionType.map:
                if (parameters.Range != null && InRange((AchievementMetadataCondition.Range<int>) parameters.Range, session.Player.Value.Character.MapId)) {
                    return true;
                }

                if (parameters.Integers != null && parameters.Integers.Contains(session.Player.Value.Character.MapId)) {
                    return true;
                }
                break;
            case AchievementConditionType.jump:
            case AchievementConditionType.meso:
            case AchievementConditionType.taxifind:
            case AchievementConditionType.fall_damage:
                return true;
            case AchievementConditionType.emotion:
                if (parameters.Strings != null && parameters.Strings.Contains(stringValue)) {
                    return true;
                }
                break;
            case AchievementConditionType.trophy_point:
                if (parameters.Range != null && InRange((AchievementMetadataCondition.Range<int>) parameters.Range, longValue)) {
                    return true;
                }
                break;
            case AchievementConditionType.interact_object:
                if ((parameters.Range != null && InRange((AchievementMetadataCondition.Range<int>) parameters.Range, longValue)) ||
                    (parameters.Integers != null && parameters.Integers.Contains((int) longValue))) {
                    if (session.Player.Value.Unlock.InteractedObjects.Contains((int) longValue)) {
                        return false;
                    }
                    session.Player.Value.Unlock.InteractedObjects.Add((int) longValue);
                    return true;
                }
                break;
            case AchievementConditionType.item_collect:
            case AchievementConditionType.item_collect_revise:
                if ((parameters.Range != null && InRange((AchievementMetadataCondition.Range<int>) parameters.Range, longValue)) ||
                    (parameters.Integers != null && parameters.Integers.Contains((int) longValue))) {
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

        bool InRange(AchievementMetadataCondition.Range<int> range, long value) {
            return value >= range.Min && value <= range.Max;
        }
    }

    private bool CheckTarget(AchievementMetadataCondition condition, long longValue = 0) {
        AchievementMetadataCondition.Parameters target = condition.Target!;
        switch (condition.Type) {
            case AchievementConditionType.map:
            case AchievementConditionType.jump:
            case AchievementConditionType.meso:
            case AchievementConditionType.taxifind:
            case AchievementConditionType.trophy_point:
            case AchievementConditionType.interact_object:
                return true;
            case AchievementConditionType.emotion:
                if (target.Range != null && target.Range.Value.Min >= session.Player.Value.Character.MapId &&
                    target.Range.Value.Max <= session.Player.Value.Character.MapId) {
                    return true;
                }
                break;
            case AchievementConditionType.fall_damage:
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
    /// <param name="achievement">Trophy entry from player</param>
    /// <param name="count">Count amount to increment on for the trophy.</param>
    /// <returns>False if there is no rank up possible or condition value has not been met.</returns>
    private bool RankUp(Achievement achievement, long count = 1) {
        achievement.Counter += count;

        if (!achievement.Metadata.Grades.TryGetValue(achievement.CurrentGrade, out AchievementMetadataGrade? grade)) {
            return false;
        }

        int newGradesCount = 0;
        while (achievement.Counter >= grade.Condition.Value) {
            if (achievement.Completed) {
                break;
            }
            achievement.Grades.Add(achievement.CurrentGrade, DateTime.Now.ToEpochSeconds());
            newGradesCount++;

            if (achievement.Grades.Count < achievement.Metadata.Grades.Count) {
                achievement.CurrentGrade++;
                Reward(achievement.Id);
            }

            // Update count on player
            switch (achievement.Category) {
                case AchievementCategory.Combat:
                    session.Player.Value.Character.AchievementInfo.Combat++;
                    break;
                case AchievementCategory.Adventure:
                    session.Player.Value.Character.AchievementInfo.Adventure++;
                    break;
                case AchievementCategory.Life:
                    session.Player.Value.Character.AchievementInfo.Lifestyle++;
                    break;
            }

            session.Send(AchievementPacket.Update(achievement));
            if (!achievement.Metadata.Grades.TryGetValue(achievement.CurrentGrade, out grade)) {
                break;
            }
        }

        if (newGradesCount > 0) {
            Update(AchievementConditionType.trophy_point, newGradesCount, codeLong: achievement.Id);
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
        if (!Values.TryGetValue(trophyId, out Achievement? trophy)) {
            return;
        }

        if (trophy.CurrentGrade < trophy.RewardGrade) {
            return;
        }

        for (int startGrade = trophy.RewardGrade; startGrade <= trophy.CurrentGrade; startGrade++) {
            if (!trophy.Metadata.Grades.TryGetValue(startGrade, out AchievementMetadataGrade? grade)) {
                continue;
            }

            if (grade.Reward == null) {
                trophy.RewardGrade++;
                continue;
            }

            if (trophy.RewardGrade == trophy.CurrentGrade && !trophy.Completed) {
                session.Send(AchievementPacket.Update(trophy));
                break;
            }

            switch (grade.Reward.Type) {
                case AchievementRewardType.item:
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
                case AchievementRewardType.title:
                    if (!manualClaim) {
                        return;
                    }

                    if (session.Player.Value.Unlock.Titles.Contains(grade.Reward.Code)) {
                        break;
                    }
                    session.Send(UserEnvPacket.AddTitle(grade.Reward.Code));
                    session.Player.Value.Unlock.Titles.Add(grade.Reward.Code);
                    break;
                case AchievementRewardType.dynamicaction:
                    if (session.Player.Value.Unlock.Emotes.Contains(grade.Reward.Code)) {
                        break;
                    }
                    session.Player.Value.Unlock.Emotes.Add(grade.Reward.Code);
                    session.Send(EmotePacket.Learn(new Emote(grade.Reward.Code)));
                    break;
                case AchievementRewardType.beauty_hair:
                case AchievementRewardType.beauty_makeup:
                case AchievementRewardType.beauty_skin:
                case AchievementRewardType.itemcoloring:
                case AchievementRewardType.shop_build:
                case AchievementRewardType.shop_ride:
                case AchievementRewardType.shop_weapon:
                case AchievementRewardType.etc: // currently used as quest unlocks
                    // I don't think anything is supposed to happen here. Just client sided visuals?
                    break;
                default:
                    logger.Error("Unimplemented trophy reward type {RewardType}", grade.Reward.Type);
                    break;
            }

            trophy.RewardGrade++;
            session.Send(AchievementPacket.Update(trophy));
        }
    }

    public void Save(GameStorage.Request db) {
        db.SaveAchievements(session.AccountId, session.CharacterId, Values.Values.ToList());
    }
}
