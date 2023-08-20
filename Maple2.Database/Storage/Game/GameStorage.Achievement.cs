using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;
using Achievement = Maple2.Model.Game.Achievement;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public IDictionary<int, Achievement> GetAccountAchievements(long accountId) {
            return Context.Achievement.Where(achievement => achievement.AccountId == accountId)
                .AsEnumerable()
                .Select(ToAchievement)
                .Where(achievement => achievement != null)
                .Where(achievement => achievement!.Metadata.AccountWide)
                .ToDictionary(achievement => achievement!.Id, achievement => achievement!);
        }

        public IDictionary<int, Achievement> GetCharacterAchievements(long characterId) {
            return Context.Achievement.Where(achievement => achievement.CharacterId == characterId)
                .AsEnumerable()
                .Select(ToAchievement)
                .Where(achievement => achievement != null)
                .Where(achievement => !achievement!.Metadata.AccountWide)
                .ToDictionary(achievement => achievement!.Id, achievement => achievement!);
        }

        public IDictionary<int, Achievement> GetCharacterAndAccountAchievements(long accountId, long characterId) {
            IDictionary<int, Achievement> accountAchievements = GetAccountAchievements(accountId);
            IDictionary<int, Achievement> characterAchievements = GetCharacterAchievements(characterId);

            foreach ((int id, Achievement achievement) in accountAchievements) {
                if (characterAchievements.ContainsKey(id)) {
                    continue;
                }
                characterAchievements.Add(id, achievement);
            }

            return characterAchievements;
        }

        public AchievementInfo GetAchievementInfo(IDictionary<int, Achievement> achievements) {
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
        
        public bool SaveAchievements(long accountId, long characterId, IList<Achievement> values) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Dictionary<int, Model.Achievement> existing = Context.Achievement.Where(model => model.AccountId == accountId)
                .ToDictionary(model => model.Id, model => model);

            foreach (Achievement value in values) {
                if (existing.TryGetValue(value.Id, out Model.Achievement? model)) {
                    model.Id = value.Id;
                    Context.Achievement.Update(model);
                } else {
                    model = value;
                    model.CharacterId = characterId;
                    model.AccountId = accountId;
                    Context.Achievement.Add(model);
                }
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
