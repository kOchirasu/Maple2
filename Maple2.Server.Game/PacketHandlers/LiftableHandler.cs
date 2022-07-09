using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class LiftableHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Liftable;

    private enum Command : byte {
        Pickup = 1,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Pickup:
                HandlePickup(session, packet);
                return;
        }
    }

    private void HandlePickup(GameSession session, IByteReader packet) {
        string entityId = packet.ReadString();
    }
}
