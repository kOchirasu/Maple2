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
        public Achievement? CreateAchievement(long ownerId, Achievement achievement) {
            Model.Achievement model = achievement;
            model.OwnerId = ownerId;
            Context.Achievement.Add(model);

            return Context.TrySaveChanges() ? ToAchievement(model) : null;
        }

        public IDictionary<int, Achievement> GetAchievements(long ownerId) {
            return Context.Achievement.Where(achievement => achievement.OwnerId == ownerId)
                .AsEnumerable()
                .Select(ToAchievement)
                .Where(achievement => achievement != null)
                .ToDictionary(achievement => achievement!.Id, achievement => achievement!);
        }

        public AchievementInfo GetAchievementInfo(long accountId, long characterId) {
            return new AchievementInfo {
                Combat = Context.Achievement
                    .Where(achievement => achievement.OwnerId == accountId || achievement.OwnerId == characterId)
                    .Where(achievement => achievement.Category == AchievementCategory.Combat)
                    .Select(achievement => achievement.CompletedCount)
                    .Sum(),
                Adventure = Context.Achievement
                    .Where(achievement => achievement.OwnerId == accountId || achievement.OwnerId == characterId)
                    .Where(achievement => achievement.Category == AchievementCategory.Adventure)
                    .Select(achievement => achievement.CompletedCount)
                    .Sum(),
                Lifestyle = Context.Achievement
                    .Where(achievement => achievement.OwnerId == accountId || achievement.OwnerId == characterId)
                    .Where(achievement => achievement.Category == AchievementCategory.Life || achievement.Category == AchievementCategory.None)
                    .Select(achievement => achievement.CompletedCount)
                    .Sum(),
            };
        }

        public bool SaveAchievements(long ownerId, ICollection<Achievement> achievements) {
            foreach (Achievement achievement in achievements) {
                Model.Achievement model = achievement;
                model.OwnerId = ownerId;

                Context.Achievement.Update(achievement);
            }

            return Context.TrySaveChanges();
        }

        // Converts model to item if possible, otherwise returns null.
        private Achievement? ToAchievement(Model.Achievement? model) {
            if (model == null) {
                return null;
            }

            return game.achievementMetadata.TryGet(model.Id, out AchievementMetadata? metadata) ? model.Convert(metadata) : null;
        }
    }
}
