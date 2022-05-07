using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.PacketHandlers;

public class CharacterInfoHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.CHARACTER_INFO;

    public CharacterInfoHandler(ILogger<CharacterInfoHandler> logger) : base(logger) { }

    public override void Handle(GameSession session, IByteReader packet) {
        long characterId = packet.ReadLong();
        session.Field?.InspectPlayer(session, characterId);
    }
}
