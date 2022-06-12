using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.PacketHandlers;

public class UserSyncHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.USER_SYNC;

    public UserSyncHandler(ILogger<UserSyncHandler> logger) : base(logger) { }

    public override void Handle(GameSession session, IByteReader packet) {
        if (session.State != SessionState.Connected) {
            return;
        }

        packet.ReadByte(); // Unknown
        packet.ReadInt(); // ServerTicks
        packet.ReadInt(); // ClientTicks

        byte segments = packet.ReadByte();
        if (segments == 0) {
            return;
        }

        var stateSyncs = new StateSync[segments];
        for (int i = 0; i < segments; i++) {
            var playerState = packet.Peek<ActorState>();
            StateSync stateSync = playerState switch {
                ActorState.MicroGameRps => new StateSyncRps(),
                ActorState.MicroGameCoupleDance => new StateSyncCoupleDance(),
                ActorState.WeddingEmotion => new StateSyncWeddingEmotion(),
                _ => new StateSync()
            };

            stateSync.ReadFrom(packet);
            stateSyncs[i] = stateSync;

            packet.ReadInt(); // ClientTicks
            packet.ReadInt(); // ServerTicks
        }

        using (var buffer = new PoolByteWriter()) {
            buffer.Player(session.Player.ObjectId, stateSyncs);
            session.Field?.Multicast(buffer, sender: session);
        }

        session.OnStateSync(stateSyncs[^1]);
    }
}
