﻿using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class BuddyBadgeHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.BuddyBadge;

    private enum Command : byte {
        Start = 0,
        Stop = 1,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var function = packet.Read<Command>();
        switch (function) {
            case Command.Start:
                HandleStart(session, packet);
                return;
            case Command.Stop:
                HandleStop(session, packet);
                return;
        }
    }

    private static void HandleStart(GameSession session, IByteReader packet) {
        long characterId = packet.ReadLong();

        if (session.Field == null) {
            return;
        }

        if (!session.Field.TryGetPlayerById(characterId, out FieldPlayer? partner)) {
            return;
        }

        if (HasBuddyBadgeEquipped(session, partner.Session)) {
            session.Field?.Broadcast(BuddyBadgePacket.Start(characterId), session);
        }
    }

    private static void HandleStop(GameSession session, IByteReader packet) {
        long characterId = packet.ReadLong();

        session.Field?.Broadcast(BuddyBadgePacket.Stop(characterId), session);
    }

    private static bool HasBuddyBadgeEquipped(GameSession sender, GameSession receiver) {
        if (!sender.Item.Equips.Badge.TryGetValue(BadgeType.Buddy, out Item? senderBadge)) {
            return false;
        }
        if (!receiver.Item.Equips.Badge.TryGetValue(BadgeType.Buddy, out Item? receiverBadge)) {
            return false;
        }

        return receiverBadge.Id != senderBadge.Id ||
               receiverBadge.CoupleInfo?.Name != receiver.Player.Value.Character.Name ||
               receiverBadge.CoupleInfo?.CharacterId != receiver.Player.Value.Character.Id ||
               senderBadge.CoupleInfo?.Name != sender.Player.Value.Character.Name ||
               receiverBadge.CoupleInfo?.CharacterId != sender.Player.Value.Character.Id;
    }
}
