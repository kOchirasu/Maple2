using Maple2.PacketLib.Tools;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.PacketHandlers;

public class ResponseKeyHandler : ResponseKeyHandler<GameSession> {
    public ResponseKeyHandler(ILogger<ResponseKeyHandler> logger) : base(logger) { }

    public override void Handle(GameSession session, IByteReader packet) {
        base.Handle(session, packet);

        // TODO: GameServer specific logic
    }

    public override string ToString() => $"[0x{OpCode:X4}] Game.{GetType().Name}";
}
