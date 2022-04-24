using Maple2.Database.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    private readonly DbContextOptions options;
    private readonly ItemMetadataStorage itemMetadata;
    private readonly ILogger logger;

    public GameStorage(DbContextOptions options, ItemMetadataStorage itemMetadata, ILogger<GameStorage> logger) {
        this.options = options;
        this.itemMetadata = itemMetadata;
        this.logger = logger;
    }

    public Request Context()  {
        return new Request(this, new Ms2Context(options), logger);
    }

    public partial class Request : DatabaseRequest<Ms2Context> {
        private readonly GameStorage game;

        public Request(GameStorage game, Ms2Context context, ILogger logger) : base(context, logger) {
            this.game = game;
        }
    }
}
