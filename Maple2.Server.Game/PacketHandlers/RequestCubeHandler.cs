using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class RequestCubeHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestCube;

    private enum Command : byte {
        BuyPlot = 2,
        ForfeitPlot = 6,
        ExtendPlot = 9,
        SetHomeName = 21,
        SetHomeMessage = 29,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.BuyPlot:
                HandleBuyPlot(session, packet);
                return;
            case Command.ForfeitPlot:
                HandleForfeitPlot(session);
                break;
            case Command.ExtendPlot:
                HandleExtendPlot(session);
                break;
            case Command.SetHomeName:
                HandleSetHomeName(session, packet);
                break;
        }
    }

    private void HandleBuyPlot(GameSession session, IByteReader packet) {
        int plotNumber = packet.ReadInt();
        packet.ReadInt(); // ApartmentNumber?

        if (session.Housing.BuyPlot(plotNumber)) {
            session.Send(CubePacket.ConfirmBuyPlot());
        }
    }

    private void HandleForfeitPlot(GameSession session) {
        Plot? plot = session.Housing.ForfeitPlot();
        if (plot != null) {
            session.Send(CubePacket.ConfirmForfeitPlot(plot));
        }
    }

    private void HandleExtendPlot(GameSession session) {
        session.Housing.ExtendPlot();
    }

    private void HandleSetHomeName(GameSession session, IByteReader packet) {
        string name = packet.ReadUnicodeString();
        if (name.Length > 16) {
            // TODO: Invalid name
            return;
        }

        session.Housing.SetHomeName(name);
    }
}
