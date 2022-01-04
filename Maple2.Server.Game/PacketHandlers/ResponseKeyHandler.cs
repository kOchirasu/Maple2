using Maple2.PacketLib.Tools;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;
using WorldClient = Maple2.Server.World.Service.World.WorldClient;

namespace Maple2.Server.Game.PacketHandlers;

public class ResponseKeyHandler : ResponseKeyHandler<GameSession> {
    public ResponseKeyHandler(WorldClient worldClient, ILogger<ResponseKeyHandler> logger) : base(worldClient, logger) { }

    public override void Handle(GameSession session, IByteReader packet) {
        base.Handle(session, packet);

        // TODO: GameServer specific logic
    }
}
