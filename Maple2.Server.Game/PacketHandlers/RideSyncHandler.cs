﻿using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.PacketHandlers;

public class RideSyncHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RideSync;

    public override void Handle(GameSession session, IByteReader packet) {
        if (session.State != SessionState.Connected || session.Field == null) {
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
            stateSyncs[i] = packet.ReadClass<StateSync>();
            packet.ReadInt(); // ClientTicks
            packet.ReadInt(); // ServerTicks
        }

        using (var buffer = new PoolByteWriter()) {
            buffer.Ride(session.Player.ObjectId, stateSyncs);
            session.Field?.Broadcast(buffer, sender: session);
        }

        session.Player.OnStateSync(stateSyncs[^1]);
    }
}
