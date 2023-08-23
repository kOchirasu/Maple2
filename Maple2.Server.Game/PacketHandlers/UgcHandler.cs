using System.Diagnostics;
using System.Linq;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class UgcHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Ugc;

    private enum Command : byte {
        ProfilePicture = 11,
        LoadCubes = 18,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.ProfilePicture:
                HandleProfilePicture(session, packet);
                break;
            case Command.LoadCubes:
                HandleLoadCubes(session, packet);
                return;
        }
    }

    private void HandleProfilePicture(GameSession session, IByteReader packet) {
        string path = packet.ReadUnicodeString();
        session.Player.Value.Character.Picture = path;

        session.Field?.Broadcast(UgcPacket.ProfilePicture(session.Player));
    }

    private void HandleLoadCubes(GameSession session, IByteReader packet) {
        int mapId = packet.ReadInt();
        Debug.Assert(mapId == session.Field?.MapId);

        lock (session.Field.Plots) {
            session.Send(LoadCubesPacket.PlotOwners(session.Field.Plots.Values));
            foreach (Plot plot in session.Field.Plots.Values) {
                if (plot.Cubes.Count > 0) {
                    session.Send(LoadCubesPacket.Load(plot));
                }
            }

            Plot[] ownedPlots = session.Field.Plots.Values.Where(plot => plot.State != PlotState.Open).ToArray();
            if (ownedPlots.Length > 0) {
                session.Send(LoadCubesPacket.PlotState(ownedPlots));
                session.Send(LoadCubesPacket.PlotExpiry(ownedPlots));
            }
        }
    }
}
