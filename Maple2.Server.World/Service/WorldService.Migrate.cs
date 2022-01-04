using System;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Grpc.Core;
using Maple2.Server.Core.Constants;
using Microsoft.Extensions.Caching.Memory;

namespace Maple2.Server.World.Service;

public partial class WorldService {
    private readonly record struct TokenEntry(long AccountId, long CharacterId);

    // Duration for which a token remains valid.
    private static readonly TimeSpan AuthExpiry = TimeSpan.FromSeconds(30);

    private readonly IMemoryCache tokenCache;

    public override Task<MigrateOutResponse> MigrateOut(MigrateOutRequest request, ServerCallContext context) {
        ulong token = UniqueToken();
        tokenCache.Set(token, new TokenEntry(request.AccountId, request.CharacterId), AuthExpiry);

        // TODO: Dynamic ip/port
        return Task.FromResult(new MigrateOutResponse {
            IpAddress = IPAddress.Loopback.ToString(),
            Port = Target.GAME_PORT,
            Token = token
        });
    }

    public override Task<MigrateInResponse> MigrateIn(MigrateInRequest request, ServerCallContext context) {
        if (!tokenCache.TryGetValue(request.Token, out TokenEntry data)) {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid token"));
        }
        if (data.AccountId != request.AccountId) {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Invalid token for account"));
        }

        tokenCache.Remove(request.Token);
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
