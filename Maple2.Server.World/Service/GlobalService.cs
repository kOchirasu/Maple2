using System;
using System.Threading.Tasks;
using Grpc.Core;
using Maple2.Database.Storage;
using Maple2.Model.Game;
using Serilog;

// TODO: Move this to a Global server
// ReSharper disable once CheckNamespace
namespace Maple2.Server.Global.Service;

public partial class GlobalService : Global.GlobalBase {
    private readonly GameStorage gameStorage;

    private readonly ILogger logger = Log.Logger.ForContext<GlobalService>();

    public GlobalService(GameStorage gameStorage) {
        this.gameStorage = gameStorage;
    }

    public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context) {
#if !DEBUG // Allow empty username for testing
        if (string.IsNullOrWhiteSpace(request.Username)) {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Username must be specified."));
        }
#endif

        // Normalize username
        string username = request.Username.Trim().ToLower();
        var passwordHash = new Guid(request.PasswordHash);

        using GameStorage.Request db = gameStorage.Context();
        Account? account = db.GetAccount(username);
        // Create account if not exists.
        if (account == null) {
            account = new Account {
                Username = username,
                PasswordHash = passwordHash,
            };

            db.BeginTransaction();
            account = db.CreateAccount(account);
            if (account == null) {
                throw new RpcException(new Status(StatusCode.Internal, "Failed to create account."));
            }

            db.Commit();
        } else {
            if (account.PasswordHash == default) {
                account.PasswordHash = passwordHash;
                db.UpdateAccount(account, true);
            } else if (account.PasswordHash != passwordHash) {
                return Task.FromResult(new LoginResponse {
                    Code = LoginResponse.Types.Code.BlockNexonSn,
                    Message = "PasswordHash mismatch",
                });
            }
        }

        return Task.FromResult(new LoginResponse{AccountId = account.Id});
    }
}
