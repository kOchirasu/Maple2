using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;

// TODO: Move this to a Global server
// ReSharper disable once CheckNamespace
namespace Maple2.Server.Global.Service;

public partial class GlobalService : Global.GlobalBase {
    private readonly ILogger logger;

    public GlobalService(ILogger<GlobalService> logger) {
        this.logger = logger;
    }

    public override Task<HealthResponse> Health(Empty request, ServerCallContext context) {
        return Task.FromResult(new HealthResponse());
    }

    public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context) {
        if (string.IsNullOrWhiteSpace(request.Username)) {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Username must be specified."));
        }

        // Normalize username
        string user = request.Username.Trim().ToLower();

        // Deterministic accountId from username.
        long accountId = 1000000000 + user.GetHashCode();
        return Task.FromResult(new LoginResponse{AccountId = accountId});
    }
}
