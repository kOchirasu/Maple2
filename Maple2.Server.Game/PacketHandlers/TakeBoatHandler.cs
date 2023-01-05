using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class TakeBoatHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.TakeBoat;

    public override void Handle(GameSession session, IByteReader packet) {
        int npcObjectId = packet.ReadInt();
        if (session.Field == null || !session.Field.Npcs.TryGetValue(npcObjectId, out FieldNpc? npc)) {
            return; // Invalid Npc
        }

        int mapId;
        int mesoCost = 0;

        switch (npc.Value.Id) {
            case 11000585: // Seamus
                mapId = 2000124;
                mesoCost = 1000;
                break;
            case 11000994: // Lotachi
                mapId = 02000183;
                mesoCost = 4000;
                break;
            case 11001257: // Moren
                mapId = 52000018;
                break;
            default:
                Logger.Warning($"Unhandled boat npc: {npc.Value.Id}");
                return;
        }

        if (session.Currency.Meso < mesoCost) {
            session.Send(ChatPacket.Alert(StringCode.s_err_lack_meso));
            return;
        }

        session.Currency.Meso -= mesoCost;
        session.Send(session.PrepareField(mapId)
            ? FieldEnterPacket.Request(session.Player)
            : FieldEnterPacket.Error(MigrationError.s_move_err_default));
    }
}
