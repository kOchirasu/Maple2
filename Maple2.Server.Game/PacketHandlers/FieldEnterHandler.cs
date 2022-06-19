using System.Diagnostics;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class FieldEnterHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.RESPONSE_FIELD_ENTER;

    public override void Handle(GameSession session, IByteReader packet) {
        Debug.Assert(packet.ReadInt() == GameSession.FIELD_KEY);

        session.EnterField();
    }
}
