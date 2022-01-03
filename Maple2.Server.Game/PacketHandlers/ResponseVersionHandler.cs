using System;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.PacketHandlers;

public class ResponseVersionHandler : ResponseVersionHandler<GameSession> {
    public ResponseVersionHandler(ILogger<ResponseVersionHandler> logger) : base(logger) { }

    public override void Handle(GameSession session, IByteReader packet) {
        base.Handle(session, packet);

        // No idea what this is, but server sends it when logging into game server
        var pWriter = Packet.Of(0x132);
        pWriter.WriteByte();
        pWriter.WriteInt(Environment.TickCount);
        session.Send(pWriter);
        session.Send(RequestPacket.Key());
    }

    public override string ToString() => $"[0x{OpCode:X4}] Game.{GetType().Name}";
}
