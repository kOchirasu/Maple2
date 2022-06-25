using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class CharacterInfoHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.CharacterInfo;

    public override void Handle(GameSession session, IByteReader packet) {
        long characterId = packet.ReadLong();

        if (session.Field?.TryGetPlayerById(characterId, out FieldPlayer? player) is true) {
            session.Send(PlayerInfoPacket.Load(player.Session));
            return;
        }

        session.Send(PlayerInfoPacket.NotFound(characterId));
    }
}
