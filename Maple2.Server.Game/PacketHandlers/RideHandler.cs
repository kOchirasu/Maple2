using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.PacketHandlers;

public class RideHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestRide;

    private enum Command : byte {
        Start = 0,
        Stop = 1,
        Change = 2,
        StartShared = 3,
        StopShared = 4,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Start:
                HandleStart(session, packet);
                return;
            case Command.Stop:
                HandleStop(session, packet);
                return;
            case Command.Change:
                HandleChange(session, packet);
                return;
            case Command.StartShared:
                HandleStartShared(session, packet);
                return;
            case Command.StopShared:
                HandleStopShared(session);
                return;
        }
    }

    private void HandleStart(GameSession session, IByteReader packet) {
        var type = packet.Read<RideOnType>();
        int mountId = packet.ReadInt();
        packet.ReadInt();
        packet.ReadLong();
        long mountUid = packet.ReadLong();
        var ugc = packet.ReadClass<UgcItemLook>();
    }

    private void HandleStop(GameSession session, IByteReader packet) {
        var type = packet.Read<RideOffType>();
        bool forced = packet.ReadBool();
    }

    private void HandleChange(GameSession session, IByteReader packet) {
        int objectId = packet.ReadInt();
        int mountId = packet.ReadInt();
        long mountUid = packet.ReadLong();
    }

    private void HandleStartShared(GameSession session, IByteReader packet) {
        int objectId = packet.ReadInt();
    }

    private void HandleStopShared(GameSession session) {

    }
}
