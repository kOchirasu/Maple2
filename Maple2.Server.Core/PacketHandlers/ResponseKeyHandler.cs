using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.Packets;
using Maple2.Server.World.Service;
using Microsoft.Extensions.Logging;
using WorldClient = Maple2.Server.World.Service.World.WorldClient;

namespace Maple2.Server.Core.PacketHandlers;

public abstract class ResponseKeyHandler<T> : PacketHandler<T> where T : Session {
    public override ushort OpCode => RecvOp.RESPONSE_KEY;

    private readonly WorldClient worldClient;

    protected ResponseKeyHandler(WorldClient worldClient, ILogger logger) : base(logger) {
        this.worldClient = worldClient;
    }

    public override void Handle(T session, IByteReader packet) {
        session.AccountId = packet.ReadLong();
        ulong token = packet.Read<ulong>();

        logger.LogInformation("LOGIN USER TO GAME: {AccountId}", session.AccountId);

        var request = new MigrateInRequest {
            AccountId = session.AccountId,
            Token = token,
        };
        var response = worldClient.MigrateIn(request);
        session.CharacterId = response.CharacterId;

        session.Send(Packet.Of(SendOp.REQUEST_SYSTEM_INFO));
        ByteWriter moveResultPacket = Packet.Of(SendOp.MOVE_RESULT);
        moveResultPacket.WriteByte();
        session.Send(moveResultPacket);
    }
}
