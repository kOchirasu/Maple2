using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.PacketHandlers;

public class BadgeEquipHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.BADGE_EQUIP;

    private enum Command : byte {
        Equip = 0,
        Unequip = 1,
    }

    public BadgeEquipHandler(ILogger<BadgeEquipHandler> logger) : base(logger) { }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Equip:
                HandleEquip(session, packet);
                return;
            case Command.Unequip:
                HandleUnequip(session, packet);
                return;
        }
    }

    private void HandleEquip(GameSession session, IByteReader packet) {
        long itemUid = packet.ReadLong();
        packet.Read<BadgeType>(); // We are using static slots based on badge type

        // Disconnect if this fails to avoid bad state.
        if (!session.Item.Equips.EquipBadge(itemUid)) {
            session.Disconnect();
        }
    }

    private void HandleUnequip(GameSession session, IByteReader packet) {
        var slot = packet.Read<BadgeType>();

        // Disconnect if this fails to avoid bad state.
        if (!session.Item.Equips.UnequipBadge(slot)) {
            session.Disconnect();
        }
    }
}
