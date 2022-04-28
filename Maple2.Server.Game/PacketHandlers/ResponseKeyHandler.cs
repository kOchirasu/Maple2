using System;
using Grpc.Core;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Session;
using Maple2.Server.World.Service;
using Microsoft.Extensions.Logging;
using WorldClient = Maple2.Server.World.Service.World.WorldClient;

namespace Maple2.Server.Game.PacketHandlers;

public class ResponseKeyHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.RESPONSE_KEY;

    private readonly WorldClient worldClient;
    private readonly GameStorage gameStorage;

    public ResponseKeyHandler(WorldClient worldClient, GameStorage gameStorage, ILogger<ResponseKeyHandler> logger) 
            : base(logger) {
        this.worldClient = worldClient;
        this.gameStorage = gameStorage;
    }

    public override void Handle(GameSession session, IByteReader packet) {
        long accountId = packet.ReadLong();
        ulong token = packet.Read<ulong>();
        var machineId = packet.Read<Guid>();

        try {
            logger.LogInformation("LOGIN USER TO GAME: {AccountId}", accountId);
            
            var request = new MigrateInRequest {
                AccountId = accountId,
                Token = token,
                MachineId = machineId.ToString(),
            };
            MigrateInResponse response = worldClient.MigrateIn(request);
            
            using GameStorage.Request db = gameStorage.Context();
            session.Account = db.GetAccount(accountId);
            session.Character = db.GetCharacter(response.CharacterId, accountId);
        } catch (RpcException) {
            session.Send(MigrationPacket.MoveResult(MigrationError.s_move_err_default));
            session.Disconnect();
            return;
        }
        
        //session.Send(Packet.Of(SendOp.REQUEST_SYSTEM_INFO));
        session.Send(MigrationPacket.MoveResult(MigrationError.ok));
            
        session.Send(TimeSyncPacket.Reset(DateTimeOffset.UtcNow));
        session.Send(TimeSyncPacket.Request());
        session.Send(RequestPacket.TickSync(Environment.TickCount));
        
        Initialize(session);
    }

    private void Initialize(GameSession session) {
        using (GameStorage.Request db = gameStorage.Context()) {
            db.GetEquips(session.Character.Id, EquipTab.Gear, EquipTab.Outfit, EquipTab.Badge);
        }
    }
}
