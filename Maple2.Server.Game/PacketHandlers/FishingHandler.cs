using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using static Maple2.Model.Error.BuddyEmoteError;

namespace Maple2.Server.Game.PacketHandlers;

public class FishingHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Fishing;

    private enum Command : byte {
        Prepare = 0,
        Stop = 1,
        Catch = 8, // Success
        Start = 9,
        FailMinigame = 10,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var function = packet.Read<Command>();
        switch (function) {
            case Command.Prepare:
                HandlePrepare(session, packet);
                break;
        }
    }

    private void HandlePrepare(GameSession session, IByteReader packet) {
        long fishingRodUid = packet.ReadLong();

        Item? rod = session.Item.Inventory.Get(fishingRodUid);
        if (rod == null || rod.Metadata.Function?.Type != ItemFunction.FishingRod) {
            session.Send(FishingPacket.Error(FishingError.s_fishing_error_invalid_item));
            return;
        }
        
        
    }
}
