using System.Linq;
using Maple2.Database.Storage;
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

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required WebStorage WebStorage { private get; init; }
    // ReSharper restore All
    #endregion

    private enum Command : byte {
        Upload = 1,
        Confirmation = 3,
        ProfilePicture = 11,
        LoadCubes = 18,
        ReserveBanner = 19,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Upload:
                HandleUpload(session, packet);
                return;
            case Command.Confirmation:
                HandleConfirmation(session, packet);
                return;
            case Command.ProfilePicture:
                HandleProfilePicture(session, packet);
                return;
            case Command.LoadCubes:
                HandleLoadCubes(session, packet);
                return;
            case Command.ReserveBanner:
                HandleReserveBanner(session, packet);
                return;
        }
    }

    private void HandleUpload(GameSession session, IByteReader packet) {
        packet.ReadLong();
        var info = packet.Read<UgcInfo>();
        packet.ReadLong();
        packet.ReadInt();
        packet.ReadShort();
        packet.ReadShort(); // -256

        switch (info.Type) {
            case UgcType.Item:
            case UgcType.Furniture:
            case UgcType.Mount:
                long itemUid = packet.ReadLong();
                int itemId = packet.ReadInt();
                int amount = packet.ReadInt();
                string name = packet.ReadUnicodeString();
                packet.ReadByte();
                long cost = packet.ReadLong();
                bool useVoucher = packet.ReadBool();
                break;
            case UgcType.Banner:
                long bannerId = packet.ReadLong();
                byte hours = packet.ReadByte();
                for (int i = 0; i < hours; i++) {
                    var reservation = packet.Read<UgcBannerReservation>();
                }
                break;
        }

        // using WebStorage.Request request = WebStorage.Context();
        // request.CreateUgc(info.CharacterId, "/path");
    }

    private void HandleConfirmation(GameSession session, IByteReader packet) {
        var info = packet.Read<UgcInfo>();
        packet.ReadInt();
        long ugcUid = packet.ReadLong();
        string ugcGuid = packet.ReadUnicodeString();
        packet.ReadShort(); // -255
    }

    private void HandleProfilePicture(GameSession session, IByteReader packet) {
        string path = packet.ReadUnicodeString();
        session.Player.Value.Character.Picture = path;

        session.Field?.Broadcast(UgcPacket.ProfilePicture(session.Player));
    }

    private void HandleLoadCubes(GameSession session, IByteReader packet) {
        int mapId = packet.ReadInt();
        if (mapId != session.Field?.MapId) {
            return;
        }

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

    private void HandleReserveBanner(GameSession session, IByteReader packet) {
        long bannerId = packet.ReadLong();
        int hours = packet.ReadInt();
        for (int i = 0; i < hours; i++) {
            var reservation = packet.Read<UgcBannerReservation>();
        }
    }
}
