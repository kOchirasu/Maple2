﻿using Grpc.Core;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Session;
using Maple2.Server.World.Service;
using static Maple2.Model.Error.MigrationError;
using WorldClient = Maple2.Server.World.Service.World.WorldClient;

namespace Maple2.Server.Game.PacketHandlers;

public class ResponseKeyHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.ResponseKey;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required WorldClient World { private get; init; }
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        long accountId = packet.ReadLong();
        ulong token = packet.Read<ulong>();
        var machineId = packet.Read<Guid>();

        try {
            Logger.Information("LOGIN USER TO GAME: {AccountId}", accountId);

            var request = new MigrateInRequest {
                AccountId = accountId,
                Token = token,
                MachineId = machineId.ToString(),
            };

            MigrateInResponse response = World.MigrateIn(request);
            if (!session.EnterServer(accountId, response.CharacterId, machineId, response.Channel)) {
                throw new InvalidOperationException($"Invalid player: {accountId}, {response.CharacterId}");
            }

            // Finalize
            session.Send(Packet.Of(SendOp.World));
        } catch (Exception ex) when (ex is RpcException or InvalidOperationException) {
            Logger.Error(ex, "Failed to login to game for accountId:{AccountId}", accountId);
            session.Send(MigrationPacket.MoveResult(s_move_err_default));
            session.Disconnect();
        }
    }
}
