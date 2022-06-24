using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Grpc.Core;
using Maple2.Server.Core.Constants;
using Maple2.Server.World.Containers;
using Microsoft.Extensions.Caching.Memory;

namespace Maple2.Server.World.Service;

public partial class WorldService {
    private readonly record struct TokenEntry(long AccountId, long CharacterId, Guid MachineId, int Channel);

    // Duration for which a token remains valid.
    private static readonly TimeSpan AuthExpiry = TimeSpan.FromSeconds(30);

    private readonly IMemoryCache tokenCache;
    private readonly PlayerChannelLookup playerChannels = new();

    public override Task<MigrateOutResponse> MigrateOut(MigrateOutRequest request, ServerCallContext context) {
        ulong token = UniqueToken();
        var entry = new TokenEntry(request.AccountId, request.CharacterId, new Guid(request.MachineId), request.Channel);
        tokenCache.Set(token, entry, AuthExpiry);
        playerChannels.Remove(request.CharacterId, out int _);

        // TODO: Dynamic ip/port
        switch (request.Server) {
            case MigrateOutRequest.Types.Server.Login:
                return Task.FromResult(new MigrateOutResponse {
                    IpAddress = Target.LOGIN_IP.ToString(),
                    Port = Target.LOGIN_PORT,
                    Token = token,
                });
            case MigrateOutRequest.Types.Server.Game:
                return Task.FromResult(new MigrateOutResponse {
                    IpAddress = Target.GAME_IP.ToString(),
                    Port = Target.GAME_PORT,
                    Token = token,
                });
            default:
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid server: {request.Server}"));
        }
    }

    public override Task<MigrateInResponse> MigrateIn(MigrateInRequest request, ServerCallContext context) {
        if (!tokenCache.TryGetValue(request.Token, out TokenEntry data)) {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid token"));
        }
        if (data.Channel != request.Channel) {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Migrating to incorrect channel"));
        }
        if (data.AccountId != request.AccountId) {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Invalid token for account"));
        }
        if (data.MachineId != new Guid(request.MachineId)) {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Mismatched machineId for account"));
        }

        tokenCache.Remove(request.Token);
        playerChannels[data.CharacterId] = request.Channel;
        return Task.FromResult(new MigrateInResponse { CharacterId = data.CharacterId });
    }

    // Generates a 64-bit token that does not exist in cache.
    private ulong UniqueToken() {
        ulong token;
        do {
            token = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));
        } while (tokenCache.TryGetValue(token, out _));

        return token;
    }
}
