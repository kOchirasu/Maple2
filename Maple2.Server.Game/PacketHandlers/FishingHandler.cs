using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Maple2.Database.Storage;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.PacketHandlers;

public class FishingHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Fishing;

    private enum Command : byte {
        Prepare = 0,
        Stop = 1,
        Catch = 8,
        Start = 9,
        FailMinigame = 10,
    }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required ItemMetadataStorage ItemMetadata { private get; init; }
    public required TableMetadataStorage TableMetadata { private get; init; }
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Prepare:
                HandlePrepare(session, packet);
                break;
            case Command.Stop:
                HandleStop(session);
                break;
            case Command.Catch:
                HandleCatch(session, packet);
                break;
            case Command.Start:
                HandleStart(session, packet);
                break;
            case Command.FailMinigame:
                HandleFailMinigame(session);
                break;
        }
    }

    private void HandlePrepare(GameSession session, IByteReader packet) {
        long fishingRodUid = packet.ReadLong();

        if (session.Field == null || session.GuideObject != null) {
            return;
        }

        Item? rod = session.Item.Inventory.Get(fishingRodUid);
        if (rod == null || rod.Metadata.Function?.Type != ItemFunction.FishingRod) {
            session.Send(FishingPacket.Error(FishingError.s_fishing_error_invalid_item));
            return;
        }

        if (!int.TryParse(rod.Metadata.Function?.Parameters, out int rodCode) || !TableMetadata.FishingRodTable.Entries.TryGetValue(rodCode, out FishingRodTable.Entry? rodMetadata)) {
            session.Send(FishingPacket.Error(FishingError.s_fishing_error_invalid_item));
            return;
        }

        if (session.Mastery[MasteryType.Fishing] < rodMetadata.MinMastery) {
            session.Send(FishingPacket.Error(FishingError.s_fishing_error_fishingrod_mastery));
            return;
        }

        if (!TableMetadata.FishingSpotTable.Entries.TryGetValue(session.Player.Value.Character.MapId, out FishingSpotTable.Entry? spotMetadata)) {
            session.Send(FishingPacket.Error(FishingError.s_fishing_error_notexist_fish));
            return;
        }

        if (session.Mastery[MasteryType.Fishing] < spotMetadata.MinMastery) {
            session.Send(FishingPacket.Error(FishingError.s_fishing_error_lack_mastery));
            return;
        }

        IList<Vector3> fishingBlocks = GetFishingBlocks(session.Player.Position, session.Player.Rotation).ToArray();
        session.GuideObject = session.Field.SpawnGuideObject(session.Player, new FishingGuideObject(), GetGuideObjectPosition(fishingBlocks, session.Player.Position));
        session.GuideObject.RodMetadata = rodMetadata;

        session.Field.Broadcast(GuideObjectPacket.Create(session.GuideObject));
        session.Send(FishingPacket.LoadTiles(fishingBlocks));
        session.Send(FishingPacket.Prepare(fishingRodUid));
    }

    private static Vector3 GetGuideObjectPosition(IEnumerable<Vector3> fishingBlocks, Vector3 playerPosition) {
        // get the fishing block closest to player
        Vector3 guidePosition = fishingBlocks.MinBy(o => Math.Sqrt(Math.Pow(playerPosition.X - o.X, 2) + Math.Pow(playerPosition.Y - o.Y, 2)));

        // Move guide 1 block up so it sits on top of the fishing block
        guidePosition.Z += 150;
        return guidePosition;
    }

    private static IEnumerable<Vector3> GetFishingBlocks(Vector3 position, Vector3 rotation) {
        /* Note this is all wrong and is a temp placeholder until the actual functionality is implemented.
         Normally how it should work is we get the intercardinal direction that the player is facing closest to (Southwest, northwest, southeast, northeast)
         We then scan in front of the player a 5x3x3 area for liquid blocks
         The liquid block CANNOT have another block on top of it 
         It cannot be ABOVE the block the player is standing on (i.e. scan the z axis same level as player and 2 blocks below max(?. May be more. Needs confirmation)
         
         For now, we will only be getting near-ish blocks. */

        Vector3 checkCoordinates = position.Align();
        float direction = rotation.AlignRotation().Z;

        checkCoordinates.Z -= Constant.BlockSize;
        Vector3 checkBlock = checkCoordinates;
        switch (direction) {
            case Constant.NorthEast: {
                checkBlock.Y += 2 * Constant.BlockSize; // start at the corner

                for (int yAxis = 0; yAxis < 5; yAxis++) {
                    for (int xAxis = 0; xAxis < 3; xAxis++) {
                        checkBlock.X += Constant.BlockSize;

                        // Normally here we would scan Z levels for liquid blocks
                        // For now we will just add the coordinate in the same level to the list;
                        yield return checkBlock;
                    }
                    checkBlock.Y -= Constant.BlockSize;
                    checkBlock.X = checkCoordinates.X; // reset X
                }
                break;
            }
            case Constant.NorthWest: {
                checkBlock.X += 2 * Constant.BlockSize; // start at the corner

                for (int xAxis = 0; xAxis < 5; xAxis++) {
                    for (int yAxis = 0; yAxis < 3; yAxis++) {
                        checkBlock.Y += Constant.BlockSize;
                        // Normally here we would scan Z levels for liquid blocks
                        // For now we will just add the coordinate in the same level to the list;
                        yield return checkBlock;
                    }
                    checkBlock.X -= Constant.BlockSize;
                    checkBlock.Y = checkCoordinates.Y; // reset Y
                }
                break;
            }
            case Constant.SouthWest: {
                checkBlock.Y -= 2 * Constant.BlockSize; // start at the corner

                for (int yAxis = 0; yAxis < 5; yAxis++) {
                    for (int xAxis = 0; xAxis < 3; xAxis++) {
                        checkBlock.X -= Constant.BlockSize;
                        // Normally here we would scan Z levels for liquid blocks
                        // For now we will just add the coordinate in the same level to the list;
                        yield return checkBlock;
                    }
                    checkBlock.Y += Constant.BlockSize;
                    checkBlock.X = checkCoordinates.X; // reset X
                }
                break;
            }
            case Constant.SouthEast: {
                checkBlock.X -= 2 * Constant.BlockSize; // start at the corner

                for (int xAxis = 0; xAxis < 5; xAxis++) {
                    for (int yAxis = 0; yAxis < 3; yAxis++) {
                        checkBlock.Y -= Constant.BlockSize;
                        // Normally here we would scan Z levels for liquid blocks
                        // For now we will just add the coordinate in the same level to the list;
                        yield return checkBlock;
                    }
                    checkBlock.X += Constant.BlockSize;
                    checkBlock.Y = checkCoordinates.Y; // reset Y
                }
                break;
            }
        }
    }

    private static void HandleStop(GameSession session) {
        if (session.Field == null || session.GuideObject == null) {
            return;
        }

        session.Send(FishingPacket.Stop());
        session.Field.Broadcast(GuideObjectPacket.Remove(session.GuideObject));
        session.GuideObject = null;
        session.FishingMiniGameActive = false;
    }

    private void HandleCatch(GameSession session, IByteReader packet) {
        bool success = packet.ReadBool();

        if (session.GuideObject == null) {
            return;
        }

        // TODO: Get the fishing block underneath the guide object and validate what fish can be caught there
        // For now we will assume all fish in the map are valid
        IEnumerable<FishTable.Entry> entries = TableMetadata.FishTable.Entries.Values.Where(entry => entry.HabitatMapIds.Contains(session.Player.Value.Character.MapId) && entry.Mastery <= session.Mastery[MasteryType.Fishing]).ToArray();
        if (!entries.Any()) {
            session.Send(FishingPacket.Error(FishingError.s_fishing_error_notexist_fish));
            HandleStop(session);
            return;
        }

        FishTable.Entry fishEntry = GetFishToCatch(entries);
        // determine size of fish
        int fishSize = Random.Shared.NextDouble() switch {
            >= 0.0 and < 0.01 when session.FishingMiniGameActive => Random.Shared.Next(fishEntry.BigSize.Max, fishEntry.BigSize.Max * 10), // prize
            >= 0.0 and < 0.03 => Random.Shared.Next(fishEntry.BigSize.Min, fishEntry.BigSize.Max), // big
            >= 0.03 and < 0.15 => Random.Shared.Next(fishEntry.SmallSize.Max, fishEntry.BigSize.Min), // medium
            >= 0.15 => Random.Shared.Next(fishEntry.SmallSize.Min, fishEntry.SmallSize.Max), // small
            _ => Random.Shared.Next(fishEntry.SmallSize.Min, fishEntry.SmallSize.Max),
        };

        FishEntry? fish = null;
        if (success) {
            IDictionary<int, FishEntry> fishAlbum = session.Player.Value.Unlock.FishAlbum;
            if (!fishAlbum.TryGetValue(fishEntry.Id, out fish)) {
                fish = new FishEntry(fishEntry.Id) {
                    LargestSize = fishSize,
                };
                fishAlbum[fishEntry.Id] = fish;
                AddMastery(session, fishEntry, CaughtFishType.FirstKind);
            }

            if (fishSize > fishEntry.BigSize.Max) {
                AddMastery(session, fishEntry, CaughtFishType.Prize);
                session.Field?.Broadcast(FishingPacket.PrizeFish(session.Player.Value.Character.Name, fishEntry.Id));
                fish.TotalPrizeFish++;
            }

            AddMastery(session, fishEntry, CaughtFishType.Default);

            fish.TotalCaught++;
            if (fishSize > fish.LargestSize) {
                fish.LargestSize = fishSize;
            }

        }

        session.Send(FishingPacket.CatchFish(fishEntry.Id, fishSize, fish));
        session.FishingMiniGameActive = false;

        if (success) {
            CatchItem(session);
        }
    }

    private void CatchItem(GameSession session) {
        if (Random.Shared.NextDouble() > Constant.FishingItemChance || TableMetadata.FishingRewardTable.Entries.Count == 0) {
            return;
        }

        FishingItemType type = Random.Shared.NextDouble() switch {
            >= 0 and < 0.825 => FishingItemType.Trash,
            >= 0.825 and < 0.975 => FishingItemType.LightBox,
            >= 0.975 and < 0.995 => FishingItemType.HeavyBox,
            >= 0.95 => FishingItemType.Skin,
            _ => FishingItemType.Trash
        };

        IEnumerable<FishingRewardTable.Entry> entries = TableMetadata.FishingRewardTable.Entries.Values.Where(x => x.Type == type).ToArray();
        IList<FishingRewardTable.Entry> items = entries.OrderBy(x => Random.Shared.Next()).Take(Constant.FishingRewardsMaxCount).ToList();

        foreach (FishingRewardTable.Entry entry in items) {
            if (!ItemMetadata.TryGet(entry.Id, out ItemMetadata? itemMetadata)) {
                continue;
            }

            if (!session.Item.Inventory.Add(new Item(itemMetadata, entry.Rarity, entry.Amount), true)) {
                return;
            }
        }

        session.Send(FishingPacket.CatchItem(items));
    }

    private void AddMastery(GameSession session, FishTable.Entry entry, CaughtFishType fishType) {

        if (session.Mastery[MasteryType.Fishing] >= Constant.FishingMasteryMax) {
            return;
        }

        int exp = entry.Rarity;
        switch (fishType) {
            case CaughtFishType.Prize:
            case CaughtFishType.FirstKind:
                exp = entry.Rarity * Constant.FishingMasteryIncreaseFactor;
                break;
            case CaughtFishType.Default:
                if (!TableMetadata.FishingSpotTable.Entries.TryGetValue(session.Player.Value.Character.MapId, out FishingSpotTable.Entry? fishingSpotEntry) ||
                    session.Mastery[MasteryType.Fishing] >= fishingSpotEntry.MaxMastery && Random.Shared.NextDouble() > Constant.FishingMasteryAdditionalExpChance) {
                    return;
                }

                // give mastery exp if within the fishing spot's mastery range
                if (fishingSpotEntry.MinMastery >= session.Mastery[MasteryType.Fishing]) {
                    exp = Random.Shared.NextDouble() switch {
                        >= 0.0 and < 0.80 => 1,
                        >= 0.80 and < 0.98 => 2,
                        >= 0.98 => 3,
                        _ => 1,
                    };
                }
                break;
        }

        session.Mastery[MasteryType.Fishing] += exp;
        session.Send(FishingPacket.IncreaseMastery(entry.Id, exp, fishType));
    }

    private static FishTable.Entry GetFishToCatch(IEnumerable<FishTable.Entry> entries) {

        List<FishTable.Entry> filteredFish;
        int rarity = 1;
        // loop until there's an acceptable rarity in the entries
        do {
            rarity = Random.Shared.NextDouble() switch {
                >= 0 and < 0.60 => 1,
                >= 0.60 and < 0.85 => 2,
                >= 0.85 and < 0.95 => 3,
                >= 0.95 => 4,
                _ => 1,
            };

            filteredFish = entries.Where(x => x.Rarity == rarity).ToList();
        } while (filteredFish.Count == 0);

        // get one fish from selected rarity
        return filteredFish.Random();
    }

    private void HandleStart(GameSession session, IByteReader packet) {
        var fishingBlock = packet.Read<Vector3B>();

        // WIP. Here we would check if the fishing block is valid and type of liquid to get proper fish.

        if (!TableMetadata.FishTable.Entries.Values.Any(x => x.HabitatMapIds.Contains(session.Player.Value.Character.MapId))) {
            session.Send(FishingPacket.Error(FishingError.s_fishing_error_notexist_fish));
            return;
        }

        int fishingTick = Constant.fisherBoreDuration;

        if (Random.Shared.NextDouble() < Constant.FishingSuccessChance) {
            if (Random.Shared.NextDouble() < Constant.FishingMiniGameChance) {
                session.FishingMiniGameActive = true;
            }
            int rodFishingTick = fishingTick - session.GuideObject?.RodMetadata?.ReduceTime ?? 0;
            fishingTick = Random.Shared.Next(rodFishingTick - rodFishingTick / 3, rodFishingTick);
        } else {
            fishingTick = Constant.fisherBoreDuration * 2; // if tick is over the base fishing tick, it will fail
        }

        session.Send(FishingPacket.Start(Environment.TickCount + fishingTick, session.FishingMiniGameActive));
    }

    private static void HandleFailMinigame(GameSession session) {
        session.FishingMiniGameActive = false;
    }
}
