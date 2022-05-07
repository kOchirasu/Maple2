using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.PacketHandlers;

public class RideSyncHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.RIDE_SYNC;

    public RideSyncHandler(ILogger<RideSyncHandler> logger) : base(logger) { }

    public override void Handle(GameSession session, IByteReader packet) {
        packet.ReadByte(); // Unknown
        packet.ReadInt(); // ServerTicks
        packet.ReadInt(); // ClientTicks

        byte segments = packet.ReadByte();
        if (segments == 0) {
            return;
        }

        var stateSyncs = new StateSync[segments];
        for (int i = 0; i < segments; i++) {
            var playerState = packet.Peek<PlayerState>();
            // TODO: Not sure if this is actually a requirement.
            if (playerState != PlayerState.Idle) {
                logger.LogError("RideSync with invalid state: {State}", playerState);
                return;
            }

            stateSyncs[i] = packet.ReadClass<StateSync>();
            packet.ReadInt(); // ClientTicks
            packet.ReadInt(); // ServerTicks
        }

        using (var buffer = new PoolByteWriter()) {
            buffer.Ride(session.Player.ObjectId, stateSyncs);
            session.Field?.Multicast(buffer, sender: session);
        }

        session.OnStateSync(stateSyncs[^1]);
    }
}
