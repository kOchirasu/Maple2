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
        
        session.Send(RequestPacket.Key());
    }
}
