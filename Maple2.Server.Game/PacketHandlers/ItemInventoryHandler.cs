using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class ItemInventoryHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestItemInventory;

    private enum Command : byte {
        Move = 3,
        Drop = 4,
        DropAll = 5,
        Sort = 10,
        Expand = 11,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Move:
                HandleMove(session, packet);
                return;
            case Command.Drop:
                HandleDrop(session, packet);
                return;
            case Command.DropAll:
                HandleDropAll(session, packet);
                return;
            case Command.Sort:
                HandleSort(session, packet);
                return;
            case Command.Expand:
                HandleExpand(session, packet);
                return;
        }
    }

    private void HandleMove(GameSession session, IByteReader packet) {
        long uid = packet.ReadLong();
        short dstSlot = packet.ReadShort();

        session.Item.Inventory.Move(uid, dstSlot);
    }

    private void HandleDrop(GameSession session, IByteReader packet) {
        if (session.Field == null) {
            return;
        }

        long uid = packet.ReadLong();
        int amount = packet.ReadInt();
        if (session.Item.Inventory.Remove(uid, out Item? removed, amount)) {
            FieldEntity<Item> fieldItem = session.Field.SpawnItem(session.Player, removed);
            session.Field.Multicast(FieldPacket.DropItem(fieldItem));
        }
    }

    private void HandleDropAll(GameSession session, IByteReader packet) {
        if (session.Field == null) {
            return;
        }

        long uid = packet.ReadLong();
        if (session.Item.Inventory.Remove(uid, out Item? removed)) {
            FieldEntity<Item> fieldItem = session.Field.SpawnItem(session.Player, removed);
            session.Field.Multicast(FieldPacket.DropItem(fieldItem));
        }
    }

    private void HandleSort(GameSession session, IByteReader packet) {
        var type = packet.Read<InventoryType>();
        bool removeExpired = packet.ReadBool();

        session.Item.Inventory.Sort(type, removeExpired);
    }

    private void HandleExpand(GameSession session, IByteReader packet) {
        var type = packet.Read<InventoryType>();

        session.Item.Inventory.Expand(type);
    }
}
