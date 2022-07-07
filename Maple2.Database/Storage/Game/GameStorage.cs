using Maple2.Database.Context;
using Maple2.Database.Model;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Home = Maple2.Database.Model.Home;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    private readonly DbContextOptions options;
    private readonly ItemMetadataStorage itemMetadata;
    private readonly MapMetadataStorage mapMetadata;
    private readonly ILogger logger;

    public GameStorage(DbContextOptions options, ItemMetadataStorage itemMetadata, MapMetadataStorage mapMetadata, ILogger<GameStorage> logger) {
        this.options = options;
        this.itemMetadata = itemMetadata;
        this.mapMetadata = mapMetadata;
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
    }

    private static PlayerInfo BuildPlayerInfo(Model.Character character, Home home, Trophy trophy) {
        return new PlayerInfo(character!, home.Plot?.MapId ?? 0, home.Plot?.Number ?? 0, home.Name, trophy);
    }
}
