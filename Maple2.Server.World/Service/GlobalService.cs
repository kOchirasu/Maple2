using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Maple2.Database.Storage;
using Maple2.Model.User;
using Microsoft.Extensions.Logging;

// TODO: Move this to a Global server
// ReSharper disable once CheckNamespace
namespace Maple2.Server.Global.Service;

public partial class GlobalService : Global.GlobalBase {
    private readonly UserStorage userStorage;
    
    private readonly ILogger logger;

    public GlobalService(UserStorage userStorage, ILogger<GlobalService> logger) {
        this.userStorage = userStorage;
        this.logger = logger;
    }

    public override Task<HealthResponse> Health(Empty request, ServerCallContext context) {
        return Task.FromResult(new HealthResponse());
    }

    public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context) {
        #if !DEBUG // Allow empty username for testing
        if (string.IsNullOrWhiteSpace(request.Username)) {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Username must be specified."));
        }
        #endif

        // Normalize username
        string username = request.Username.Trim().ToLower();

        using UserStorage.Request db = userStorage.Context();
        Account account = db.GetAccount(username);
        // Create account if not exists.
        if (account == null) {
            account = new Account {Username = username};
            account = db.CreateAccount(account);
        }
        
        return Task.FromResult(new LoginResponse{AccountId = account.Id});
    }
}
