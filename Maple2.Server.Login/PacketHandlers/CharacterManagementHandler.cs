using System;
using System.Net;
using Grpc.Core;
using Maple2.Model.Error;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Login.Session;
using Maple2.Server.World.Service;
using Microsoft.Extensions.Logging;
using WorldClient = Maple2.Server.World.Service.World.WorldClient;

namespace Maple2.Server.Login.PacketHandlers;

public class CharacterManagementHandler : PacketHandler<LoginSession> {
    public override ushort OpCode => RecvOp.CHARACTER_MANAGEMENT;

    private enum Type : byte {
        Select = 0,
        Create = 1,
        Delete = 2,
        CancelDelete = 3,
    }

    private readonly WorldClient world;

    public CharacterManagementHandler(WorldClient world, ILogger<CharacterManagementHandler> logger) : base(logger) {
        this.world = world;
    }

    public override void Handle(LoginSession session, IByteReader packet) {
        var type = packet.Read<Type>();
        switch (type) {
            case Type.Select:
                HandleSelect(session, packet);
                break;
            case Type.Create:
                HandleCreate(session, packet);
                break;
            case Type.Delete:
                HandleDelete(session, packet);
                break;
            case Type.CancelDelete:
                HandleCancelDelete(session, packet);
                break;
            default:
                throw new ArgumentException($"Invalid CHARACTER_MANAGEMENT type {type}");
        }
    }

    private void HandleSelect(LoginSession session, IByteReader packet) {
        long characterId = packet.ReadLong();
        packet.ReadShort(); // 01 00

        try {
            var request = new MigrateOutRequest {
                AccountId = session.AccountId,
                CharacterId = characterId
            };
            
            logger.LogInformation("Logging in to game as {Request}", request);
            
            MigrateOutResponse response = world.MigrateOut(request);
            var endpoint = new IPEndPoint(IPAddress.Parse(response.IpAddress), response.Port);
            
            session.Send(MigrationPacket.LoginToGame(endpoint, response.Token, 2000062));
            session.Disconnect();
        } catch (RpcException ex) {
            session.Send(MigrationPacket.LoginToGameError(MigrationError.s_move_err_default, ex.Message));
        }
    }

    private void HandleCreate(LoginSession session, IByteReader packet) {
        
    }
    
    private void HandleDelete(LoginSession session, IByteReader packet) {
        
    }
    
    private void HandleCancelDelete(LoginSession session, IByteReader packet) {
        
    }
}
