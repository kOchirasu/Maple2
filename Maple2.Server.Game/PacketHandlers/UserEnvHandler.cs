using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class UserEnvHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestUserEnv;

    private enum Command : byte {
        ChangeTitle = 1,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.ChangeTitle:
                HandleChangeTitle(session, packet);
                break;
        }
    }

    private void HandleChangeTitle(GameSession session, IByteReader packet) {
        int titleId = packet.ReadInt();

        if (titleId != 0 && !session.Player.Value.Unlock.Titles.Contains(titleId)) {
            return;
        }

        session.Player.Value.Character.Title = titleId;
        session.Field?.Broadcast(UserEnvPacket.UpdateTitle(session.Player.ObjectId, titleId));
    }
}
