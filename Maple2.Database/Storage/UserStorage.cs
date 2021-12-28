using Maple2.Database.Context;
using Maple2.Database.Extensions;
using Maple2.Model.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maple2.Database.Storage;

public partial class UserStorage {
    private readonly DbContextOptions options;
    private readonly ILogger logger;

    public UserStorage(DbContextOptions options, ILogger<UserStorage> logger) {
        this.options = options;
        this.logger = logger;
    }

    public Request Context()  {
        return new Request(this, new TestContext(options), logger);
    }

    public partial class Request : DatabaseRequest<TestContext> {
        private readonly UserStorage storage;

        public Request(UserStorage storage, TestContext context, ILogger logger) : base(context, logger) {
            this.storage = storage;
        }

        public Account GetAccount(long accountId) {
            return context.Account.Find(accountId);
        }

        public Account CreateAccount(Account account) {
            Maple2.Database.Schema.Account model = account;
            model.Id = 0;
            context.Account.Add(model);
            return context.TrySaveChanges() ? model : null;
        }
    }
}
