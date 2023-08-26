using Maple2.Database.Context;
using Maple2.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UgcResource = Maple2.Model.Game.UgcResource;

namespace Maple2.Database.Storage;

public partial class WebStorage {
    private readonly DbContextOptions options;
    private readonly ILogger logger;

    public WebStorage(DbContextOptions options, ILogger<WebStorage> logger) {
        this.options = options;
        this.logger = logger;
    }

    public Request Context() {
        // We use NoTracking by default since most requests are Read or Overwrite.
        // If we need tracking for modifying data, we can set it individually as needed.
        var context = new WebContext(options);
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        return new Request(context, logger);
    }

    public partial class Request : DatabaseRequest<WebContext> {
        public Request(WebContext context, ILogger logger) : base(context, logger) { }

        public UgcResource? CreateUgc(long ownerId, string path) {
            var resource = new Model.UgcResource {
                OwnerId = ownerId,
                Path = path,
            };
            Context.UgcResource.Add(resource);

            return Context.TrySaveChanges() ? resource : null;
        }

        public UgcResource? GetUgc(long id) {
            return Context.UgcResource.Find(id);
        }
    }
}
