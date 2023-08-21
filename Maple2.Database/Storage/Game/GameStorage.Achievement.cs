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

        private IDictionary<int, Achievement> GetCharacterAndAccountAchievements(long accountId, long characterId) {
            IDictionary<int, Achievement> accountAchievements = GetAchievements(accountId);
            IDictionary<int, Achievement> characterAchievements = GetAchievements(characterId);

            foreach ((int id, Achievement achievement) in accountAchievements) {
                if (characterAchievements.ContainsKey(id)) {
                    continue;
                }
                characterAchievements.Add(id, achievement);
            }

            return characterAchievements;
        }

        public AchievementInfo GetAchievementInfo(long accountId, long characterId) {
            IDictionary<int, Achievement> achievements = GetCharacterAndAccountAchievements(accountId, characterId);
            AchievementInfo info = new AchievementInfo();
            foreach (Achievement trophy in achievements.Values) {
                switch (trophy.Category) {
                    case AchievementCategory.Combat:
                        info.Combat += trophy.Grades.Count;
                        break;
                    case AchievementCategory.Adventure:
                        info.Adventure += trophy.Grades.Count;
                        break;
                    case AchievementCategory.None:
                    case AchievementCategory.Life:
                        info.Lifestyle += trophy.Grades.Count;
                        break;
                }
            }
            return info;
        }

        public bool SaveAchievements(IList<Achievement> achievements) {
            foreach (Achievement achievement in achievements) {
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
