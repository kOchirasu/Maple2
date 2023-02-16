using System;
using System.Linq;
using System.Numerics;
using IronPython.Modules;
using Maple2.Database.Storage;
using Maple2.Model.Error;
using Maple2.Model.Game.Event;
using Maple2.Model.Metadata;
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

        gameEvent.Tiles = gameEvent.Tiles.OrderBy(tile => tile.Position).ToList();

        var function = packet.Read<Command>();
        switch (function) {
            case Command.Load:
                HandleLoad(session, gameEvent);
                return;
            case Command.Roll:
                HandleRoll(session, gameEvent);
                return;
        }
    }

    private void HandleLoad(GameSession session, BlueMarble gameEvent) {
        // TODO: Get players stats
        session.Send(MapleopolyPacket.Load(gameEvent.Tiles));
    }

    private void HandleRoll(GameSession session, BlueMarble gameEvent) {
        
        // Check if player can roll
        var tickets = session.Item.Inventory.Find(Constant.MapleopolyTicketItemId);
        int ticketAmount = tickets.Sum(ticket => ticket.Amount);
        
        // TODO: Check if player has free rolls. If yes, use first
        if (ticketAmount >= Constant.MapleopolyTicketCostCount) {
            // consume
        } else {
            
        }
    }
}
