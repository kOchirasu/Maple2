using System.Diagnostics;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class LoadUgcMapHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestLoadUgcMap;

    public override void Handle(GameSession session, IByteReader packet) {
        Debug.Assert(packet.ReadInt() == GameSession.FIELD_KEY);
        if (session.Field == null) {
            return;
        }

        // TODO: This is treating home map as users home always.
        if (session.Player.Value.Home.MapId == session.Field.MapId) {
            if (session.Field.Plots.TryGetValue(session.Player.Value.Home.Number, out Plot? plot)) {
                LoadUgcMapPacket.LoadHome(session.Player.Value.Home, plot.Cubes.Count);
                return;
            }
        }

        session.Send(LoadUgcMapPacket.Load());
    }
}
