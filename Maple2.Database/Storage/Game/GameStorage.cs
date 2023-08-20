using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Context;
using Maple2.Database.Extensions;
using Maple2.Database.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Achievement = Maple2.Database.Model.Achievement;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    private readonly DbContextOptions options;
    private readonly ItemMetadataStorage itemMetadata;
    private readonly MapMetadataStorage mapMetadata;
    private readonly AchievementMetadataStorage achievementMetadata;
    private readonly ILogger logger;

    public GameStorage(DbContextOptions options, ItemMetadataStorage itemMetadata, MapMetadataStorage mapMetadata, AchievementMetadataStorage achievementMetadata, ILogger<GameStorage> logger) {
        this.options = options;
        this.itemMetadata = itemMetadata;
        this.mapMetadata = mapMetadata;
        this.achievementMetadata = achievementMetadata;
        this.logger = logger;
    }

    public Request Context() {
        // We use NoTracking by default since most requests are Read or Overwrite.
        // If we need tracking for modifying data, we can set it individually as needed.
        var context = new Ms2Context(options);
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        return new Request(this, context, logger);
    }

    public partial class Request : DatabaseRequest<Ms2Context> {
        private readonly GameStorage game;

        public Request(GameStorage game, Ms2Context context, ILogger logger) : base(context, logger) {
            this.game = game;
        }
        
        private static PlayerInfo BuildPlayerInfo(Model.Character character, UgcMap indoor, UgcMap? outdoor, IEnumerable<Achievement> accountAchievements, IEnumerable<Achievement> characterAchievements) {
            AchievementInfo achievements = new AchievementInfo();
            foreach (Achievement trophy in accountAchievements) {
                if (trophy.CharacterId == character.Id) {
                    continue;
                }
                switch (trophy.Category) {
                    case AchievementCategory.Combat:
                        achievements.Combat += trophy.Grades.Count;
                        break;
                    case AchievementCategory.Adventure:
                        achievements.Adventure += trophy.Grades.Count;
                        break;
                    case AchievementCategory.None:
                    case AchievementCategory.Life:
                        achievements.Lifestyle += trophy.Grades.Count;
                        break;
                }
            }

            foreach (Achievement trophy in characterAchievements) {
                switch (trophy.Category) {
                    case AchievementCategory.Combat:
                        achievements.Combat += trophy.Grades.Count;
                        break;
                    case AchievementCategory.Adventure:
                        achievements.Adventure += trophy.Grades.Count;
                        break;
                    case AchievementCategory.None:
                    case AchievementCategory.Life:
                        achievements.Lifestyle += trophy.Grades.Count;
                        break;
                }
            }
            
            if (outdoor == null) {
                return new PlayerInfo(character, indoor.Name, achievements);
            }

            return new PlayerInfo(character, outdoor.Name, achievements) {
                PlotMapId = outdoor.MapId,
                PlotNumber = outdoor.Number,
                ApartmentNumber = outdoor.ApartmentNumber,
                PlotExpiryTime = outdoor.ExpiryTime.ToEpochSeconds(),
            };
        }
    }


}
