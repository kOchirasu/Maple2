using System.Collections.Generic;
using Maple2.Database.Context;
using Maple2.Database.Extensions;
using Maple2.Database.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrophyEntry = Maple2.Database.Model.TrophyEntry;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    private readonly DbContextOptions options;
    private readonly ItemMetadataStorage itemMetadata;
    private readonly MapMetadataStorage mapMetadata;
    private readonly TrophyMetadataStorage trophyMetadata;
    private readonly ILogger logger;

    public GameStorage(DbContextOptions options, ItemMetadataStorage itemMetadata, MapMetadataStorage mapMetadata, TrophyMetadataStorage trophyMetadata, ILogger<GameStorage> logger) {
        this.options = options;
        this.itemMetadata = itemMetadata;
        this.mapMetadata = mapMetadata;
        this.trophyMetadata = trophyMetadata;
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
        
        private static PlayerInfo BuildPlayerInfo(Model.Character character, UgcMap indoor, UgcMap? outdoor, IDictionary<int, TrophyEntry> trophy) {
            Trophy playerTrophy = new Trophy();
            foreach ((int id, TrophyEntry entry) in trophy) {
                switch (entry.Category) {
                    case TrophyCategory.Combat:
                        playerTrophy.Combat += entry.Grades.Count;
                        break;
                    case TrophyCategory.Adventure:
                        playerTrophy.Adventure += entry.Grades.Count;
                        break;
                    case TrophyCategory.None:
                    case TrophyCategory.Life:
                        playerTrophy.Lifestyle += entry.Grades.Count;
                        break;
                }
            }
            
            if (outdoor == null) {
                return new PlayerInfo(character, indoor.Name, playerTrophy);
            }

            return new PlayerInfo(character, outdoor.Name, playerTrophy) {
                PlotMapId = outdoor.MapId,
                PlotNumber = outdoor.Number,
                ApartmentNumber = outdoor.ApartmentNumber,
                PlotExpiryTime = outdoor.ExpiryTime.ToEpochSeconds(),
            };
        }
    }


}
