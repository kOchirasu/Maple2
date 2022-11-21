using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class LapenshardHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Lapenshard;

    private enum Command : byte {
        Equip = 1,
        Unequip = 2,
        AddLapenshard = 3,
        AddCatalyst = 4,
        Upgrade = 5,
        Unknown = 6,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Equip:
                HandleEquip(session, packet);
                return;
            case Command.Unequip:
                HandleUnequip(session, packet);
                return;
            case Command.AddLapenshard:
                HandleAddLapenshard(session, packet);
                return;
            case Command.AddCatalyst:
                HandleAddCatalyst(session, packet);
                return;
            case Command.Upgrade:
                HandleUpgrade(session, packet);
                return;
            case Command.Unknown:
                HandleUnknown(session, packet);
                return;
        }
    }

    private void HandleEquip(GameSession session, IByteReader packet) {
        int slot = packet.ReadInt();
        long itemUid = packet.ReadLong();
    }

    private void HandleUnequip(GameSession session, IByteReader packet) {
        int slot = packet.ReadInt();
    }

    private void HandleAddLapenshard(GameSession session, IByteReader packet) {
        long itemUid = packet.ReadLong();
        int itemId = packet.ReadInt();
        int slot = packet.ReadInt();
    }

    private void HandleAddCatalyst(GameSession session, IByteReader packet) {
        long itemUid = packet.ReadLong();
        int itemId = packet.ReadInt();
        int slot = packet.ReadInt();
        int amount = packet.ReadInt();
    }

    private void HandleUpgrade(GameSession session, IByteReader packet) {
        long lapenshardUid = packet.ReadLong();
        int lapenshardId = packet.ReadInt();
        int slot = packet.ReadInt();
        int count = packet.ReadInt();
        for (int i = 0; i < count; i++) {
            packet.ReadLong(); // Uid
            packet.ReadInt(); // Amount
        }
    }

    private void HandleUnknown(GameSession session, IByteReader packet) {
        int itemId = packet.ReadInt();
    }
}
