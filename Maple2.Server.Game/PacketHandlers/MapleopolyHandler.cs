using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Game.Event;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class MapleopolyHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Mapleopoly;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required ItemMetadataStorage ItemMetadata { private get; init; }
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
        GameEvent? gameEvent = db.FindEvent(nameof(BlueMarble));
        var blueMarble = gameEvent?.EventInfo as BlueMarble;
        if (blueMarble == null || gameEvent == null) {
            // TODO: Find error to state event is not active
            return;
        }

        blueMarble.Tiles = blueMarble.Tiles.OrderBy(tile => tile.Position).ToList();

        GameEventUserValue freeRollValue = session.Config.GetGameEventUserValue(GameEventUserValueType.MapleopolyFreeRollAmount, gameEvent);
        GameEventUserValue totalTileValue = session.Config.GetGameEventUserValue(GameEventUserValueType.MapleopolyTotalTileCount, gameEvent);

        var function = packet.Read<Command>();
        switch (function) {
            case Command.Load:
                HandleLoad(session, blueMarble, totalTileValue, freeRollValue);
                return;
            case Command.Roll:
                HandleRoll(session, totalTileValue, freeRollValue);
                return;
            case Command.Result:
                HandleResult(session, gameEvent, totalTileValue, freeRollValue);
                return;
        }
    }

    private void HandleLoad(GameSession session, BlueMarble blueMarble, GameEventUserValue totalTileValue, GameEventUserValue freeRollValue) {
        var tickets = session.Item.Inventory.Find(Constant.MapleopolyTicketItemId);
        int ticketAmount = tickets.Sum(ticket => ticket.Amount);

        int.TryParse(totalTileValue.Value, out int totalTiles);
        int.TryParse(freeRollValue.Value, out int freeRolls);
        session.Send(MapleopolyPacket.Load(blueMarble.Tiles, totalTiles, freeRolls, ticketAmount));
    }

    private void HandleRoll(GameSession session, GameEventUserValue totalTileValue, GameEventUserValue freeRollValue) {
        int.TryParse(freeRollValue.Value, out int freeRolls);

        if (freeRolls > 0) {
            freeRolls--;
        } else {
            if (!session.Item.Inventory.ConsumeItemComponents(new List<ItemComponent> {
                    new(ItemId: Constant.MapleopolyTicketItemId,
                        Amount: Constant.MapleopolyTicketCostCount,
                        Rarity: Constant.MapleopolyTicketRarity,
                        Tag: ItemTag.None),
                })) {
                session.Send(MapleopolyPacket.Error(MapleopolyError.s_bluemarble_result_consume_fail));
                return;
            }
        }

        // roll two dice
        int dice1 = Random.Shared.Next(1, 7);
        int dice2 = Random.Shared.Next(1, 7);

        int.TryParse(totalTileValue.Value, out int totalTiles);
        totalTiles += dice1 + dice2;
        if (dice1 == dice2) {
            freeRolls++;
        }

        session.Config.UpdateGameEventUserValue(GameEventUserValueType.MapleopolyFreeRollAmount, freeRolls);
        session.Config.UpdateGameEventUserValue(GameEventUserValueType.MapleopolyTotalTileCount, totalTiles);
        session.Send(MapleopolyPacket.Roll(totalTiles, dice1, dice2));
    }

    private void HandleResult(GameSession session, GameEvent gameEvent, GameEventUserValue totalTileValue, GameEventUserValue freeRollValue) {
        int.TryParse(freeRollValue.Value, out int freeRolls);
        int.TryParse(totalTileValue.Value, out int totalTiles);
        if (gameEvent.EventInfo is not BlueMarble blueMarble) {
            return;
        }
        int currentTilePosition = totalTiles % blueMarble.Tiles.Count;

        BlueMarbleTile tile = blueMarble.Tiles[currentTilePosition];

        switch (tile.Type) {
            case BlueMarbleTileType.Item:
            case BlueMarbleTileType.TreasureTrove:
                if (tile.Item == null || !ItemMetadata.TryGet(tile.Item.Value.ItemId, out ItemMetadata? itemMetadata)) {
                    // TODO: Error
                    break;
                }
                var item = new Item(itemMetadata, tile.Item.Value.ItemRarity, tile.Item.Value.ItemAmount);
                if (!session.Item.Inventory.Add(item, true)) {
                    // TODO: Mail to user
                }
                break;
            case BlueMarbleTileType.Backtrack:
                totalTiles -= tile.MoveAmount;
                break;
            case BlueMarbleTileType.MoveForward:
                totalTiles += tile.MoveAmount;
                break;
            case BlueMarbleTileType.RoundTrip:
                totalTiles += blueMarble.Tiles.Count;
                break;
            case BlueMarbleTileType.GoToStart:
                totalTiles += blueMarble.Tiles.Count - currentTilePosition;
                break;
            case BlueMarbleTileType.Start:
                // Do nothing
                break;
            case BlueMarbleTileType.Lose:
            case BlueMarbleTileType.RollAgain:
            case BlueMarbleTileType.Trap:
            default:
                Logger.Warning($"Unhandled Mapleopoly tile type: {tile.Type}");
                break;
        }

        GameEventUserValue totalTrips = session.Config.GetGameEventUserValue(GameEventUserValueType.MapleopolyTotalTrips, gameEvent);
        int.TryParse(totalTrips.Value, out int trips);
        int totalNewTrips = totalTiles / blueMarble.Tiles.Count;

        if (totalNewTrips > trips) {
            int difference = totalNewTrips - trips;

            for (int tripCount = 0; tripCount < difference; tripCount++) {
                trips++;

                // Check if there's any item to give for every 1 trip
                var entry1 = blueMarble.Entries.FirstOrDefault(entry => entry.TripAmount == 0);
                if (entry1 != default && ItemMetadata.TryGet(entry1.Item.ItemId, out ItemMetadata? trip0Metadata)) {
                    var trip0Item = new Item(trip0Metadata, entry1.Item.ItemRarity, entry1.Item.ItemAmount);
                    if (!session.Item.Inventory.Add(trip0Item, true)) {
                        // TODO: mail to player
                    }
                }

                // Check if there's any other item to give for hitting a specific number of trips
                var entry2 = blueMarble.Entries.FirstOrDefault(entry => entry.TripAmount == trips);
                if (entry2 == default || !ItemMetadata.TryGet(entry2.Item.ItemId, out ItemMetadata? tripMetadata)) {
                    continue;
                }
                var tripItem = new Item(tripMetadata, entry2.Item.ItemRarity, entry2.Item.ItemAmount);
                if (!session.Item.Inventory.Add(tripItem, true)) {
                    // TODO: mail to player
                }
            }

            session.Config.UpdateGameEventUserValue(GameEventUserValueType.MapleopolyTotalTrips, trips);
        }
        session.Config.UpdateGameEventUserValue(GameEventUserValueType.MapleopolyTotalTileCount, totalTiles);
        session.Send(MapleopolyPacket.Result(tile, totalTiles, freeRolls));
    }
}
