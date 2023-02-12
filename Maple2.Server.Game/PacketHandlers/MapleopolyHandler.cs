using System;
using System.Numerics;
using Maple2.Database.Storage;
using Maple2.Model.Error;
using Maple2.Model.Game.Event;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using static Maple2.Model.Error.BuddyEmoteError;

namespace Maple2.Server.Game.PacketHandlers;

public class MapleopolyHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Mapleopoly;
    
    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required GameStorage GameStorage { private get; init; }
    // ReSharper restore All
    #endregion

    private enum Command : byte {
        Load = 0,
        Roll = 1,
        Result = 3,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        using GameStorage.Request db = GameStorage.Context();
        if (db.FindEvent(nameof(BlueMarble))?.EventInfo is not BlueMarble gameEvent) {
            // TODO: Find error to state event is not active
            return;
        }

        var function = packet.Read<Command>();
        switch (function) {
            case Command.Load:
                HandleLoad(session, gameEvent);
                return;
        }
    }

    private void HandleLoad(GameSession session, BlueMarble gameEvent) {
        session.Send(MapleopolyPacket.Load(gameEvent.Tiles));
    }
}
