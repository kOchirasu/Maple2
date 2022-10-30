using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class PetInventoryHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestPetInventory;

    private enum Command : byte {
        Add = 0,
        Remove = 1,
        Move = 3,
        Load = 7,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();

        switch (command) {
            case Command.Add:
                HandleAdd(session, packet);
                return;
            case Command.Remove:
                HandleRemove(session, packet);
                return;
            case Command.Move:
                HandleMove(session, packet);
                return;
            case Command.Load:
                HandleLoad(session);
                return;
        }
    }

    private static void HandleAdd(GameSession session, IByteReader packet) {
        long itemUid = packet.ReadLong();
        short slot = packet.ReadShort();
        int amount = packet.ReadInt();

        session.Pet?.Add(itemUid, slot, amount);
    }

    private static void HandleRemove(GameSession session, IByteReader packet) {
        long itemUid = packet.ReadLong();
        short slot = packet.ReadShort();
        int amount = packet.ReadInt();

        session.Pet?.Remove(itemUid, slot, amount);
    }

    private static void HandleMove(GameSession session, IByteReader packet) {
        long itemUid = packet.ReadLong();
        short dstSlot = packet.ReadShort();

        session.Pet?.Move(itemUid, dstSlot);
    }

    private static void HandleLoad(GameSession session) {
        session.Pet?.LoadInventory();
    }
}
