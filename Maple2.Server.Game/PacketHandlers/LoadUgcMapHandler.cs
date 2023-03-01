using System.Diagnostics;
using Maple2.Database.Storage;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class LoadUgcMapHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestLoadUgcMap;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required GameStorage GameStorage { private get; init; }
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        Debug.Assert(packet.ReadInt() == GameSession.FIELD_KEY);
        if (session.Field == null) {
            return;
        }

        if (session.Field.MapId == Constant.DefaultHomeMapId && session.Field.OwnerId > 0) {
            using GameStorage.Request db = GameStorage.Context();
            Home? home = db.GetHome(session.Field.OwnerId);
            if (home != null) {
                // Technically this sends home details to all players who enter map (including passcode)
                // but you would already know passcode if you entered the map.
                session.Send(LoadUgcMapPacket.LoadHome(home));
            }
            return;
        }

        session.Send(LoadUgcMapPacket.Load());
    }
}
