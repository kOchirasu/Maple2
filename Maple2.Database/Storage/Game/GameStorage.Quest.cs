using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Plus;
using Achievement = Maple2.Model.Game.Achievement;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public Quest? CreateQuest(long ownerId, Quest achievement) {
            Model.Quest model = achievement;
            model.OwnerId = ownerId;
            Context.Quest.Add(model);

            return Context.TrySaveChanges() ? ToQuest(model) : null;
        }

        /*public IDictionary<int, Achievement> GetAchievements(long ownerId) {
            return Context.Achievement.Where(achievement => achievement.OwnerId == ownerId)
                .AsEnumerable()
                .Select(ToAchievement)
                .Where(achievement => achievement != null)
                .ToDictionary(achievement => achievement!.Id, achievement => achievement!);
        }

        public AchievementInfo GetAchievementInfo(long accountId, long characterId) {
            Dictionary<AchievementCategory, int> achievementCounts = Context.Achievement
                .Where(achievement => achievement.OwnerId == accountId || achievement.OwnerId == characterId)
                .GroupBy(achievement => achievement.Category)
                .Select(group => new {
                    Category = group.Key,
                    Count = group.Sum(g => g.CompletedCount),
                })
                .ToDictionary(entry => entry.Category, entry => entry.Count);

            return new AchievementInfo {
                Combat = achievementCounts.GetValueOrDefault(AchievementCategory.Combat, 0),
                Adventure = achievementCounts.GetValueOrDefault(AchievementCategory.Adventure, 0),
                Lifestyle = achievementCounts.GetValueOrDefault(AchievementCategory.Life, 0)
                            + achievementCounts.GetValueOrDefault(AchievementCategory.None, 0),
            };
        }

        public bool SaveAchievements(long ownerId, ICollection<Achievement> achievements) {
            foreach (Achievement achievement in achievements) {
                Model.Achievement model = achievement;
                model.OwnerId = ownerId;

                Context.Achievement.Update(model);
            }

            return Context.TrySaveChanges();
        }*/
        
        public IDictionary<int, Quest> GetQuests(long ownerId) {
            return Context.Quest.Where(quest => quest.OwnerId == ownerId)
                .AsEnumerable()
                .Select(ToQuest)
                .Where(quest => quest != null)
                .ToDictionary(quest => quest!.Id, quest => quest!);
        }
        
        public bool DeleteQuest(long ownerId, int questId) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Model.Quest? quest = Context.Quest.Find(ownerId, questId);
            if (quest == null) {
                return false;
            }

            Context.Quest.Remove(quest);
            return SaveChanges();
        }

        public bool SaveQuests(long ownerId, ICollection<Quest> quests) {
            foreach (Quest quest in quests) {
                Model.Quest model = quest;
                model.OwnerId = ownerId;

                Context.Quest.Update(model);
            }

            return Context.TrySaveChanges();
        }

        // Converts model to item if possible, otherwise returns null.
        private Quest? ToQuest(Model.Quest? model) {
            if (model == null) {
                return null;
            }

            return game.questMetadata.TryGet(model.Id, out QuestMetadata? metadata) ? model.Convert(metadata) : null;
        }
    }
}
