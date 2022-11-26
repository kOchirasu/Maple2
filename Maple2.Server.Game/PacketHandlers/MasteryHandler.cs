using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Storage;
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

public class MasteryHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Mastery;

    private enum Command : byte {
        Reward = 1,
        Craft = 2,
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
            case Command.Reward:
                HandleReward(session, packet);
                break;
            case Command.Craft:
                HandleCraft(session, packet);
                break;
        }
    }

    private void HandleReward(GameSession session, IByteReader packet) {
        int rewardBoxDetails = packet.ReadInt();
        var type = (MasteryType) (rewardBoxDetails / 1000);
        int level = rewardBoxDetails % 100;

        if (session.Player.Value.Unlock.MasteryRewardsClaimed.TryGetValue(rewardBoxDetails, out bool isClaimed) && isClaimed) {
            session.Send(MasteryPacket.Error(MasteryError.s_mastery_error_unknown));
            return;
        }

        if (!TableMetadata.MasteryRewardTable.Entries.TryGetValue(type, level, out MasteryRewardTable.Entry? entry)) {
            return;
        }

        if (session.Mastery[type] < entry.Value) {
            session.Send(MasteryPacket.Error(MasteryError.s_mastery_error_invalid_level));
            return;
        }

        if (!ItemMetadata.TryGet(entry.ItemId, out ItemMetadata? itemMetadata)) {
            return;
        }

        var rewardItem = new Item(itemMetadata, entry.ItemRarity, entry.ItemAmount);

        if (!session.Item.Inventory.Add(rewardItem, true)) {
            session.Send(ChatPacket.Alert(StringCode.s_err_inventory));
            return;
        }

        session.Player.Value.Unlock.MasteryRewardsClaimed.Add(rewardBoxDetails, true);
        session.Send(MasteryPacket.ClaimReward(rewardBoxDetails, new List<MasteryRecipeTable.Ingredient>() {
            new(entry.ItemId, (short) entry.ItemRarity, entry.ItemRarity, ItemTag.None),
        }));
    }

    private void HandleCraft(GameSession session, IByteReader packet) {
        int recipeId = packet.ReadInt();

        if (!TableMetadata.MasteryRecipeTable.Entries.TryGetValue(recipeId, out MasteryRecipeTable.Entry? entry)) {
            return;
        }

        // TODO: Check if player has completed the required quests.

        if (session.Mastery[entry.Type] < entry.RequiredMastery) {
            session.Send(MasteryPacket.Error(MasteryError.s_mastery_error_lack_mastery));
        }

        if (session.Currency.CanAddMeso(-entry.RequiredMeso) != -entry.RequiredMeso) {
            session.Send(MasteryPacket.Error(MasteryError.s_mastery_error_lack_meso));
            return;
        }

        Dictionary<int, List<Item>> materialsById = entry.RequiredItems.ToDictionary(
            ingredient => ingredient.ItemId,
            ingredient => session.Item.Inventory.Find(ingredient.ItemId, ingredient.Rarity).ToList()
        );
        Dictionary<ItemTag, IList<Item>> materialsByTag = entry.RequiredItems.ToDictionary(
            ingredient => ingredient.Tag,
            ingredient => session.Item.Inventory.Filter(item => item.Metadata.Property.Tag == ingredient.Tag)
        );

        lock (session.Item) {
            foreach (MasteryRecipeTable.Ingredient ingredient in entry.RequiredItems) {
                int remaining = ingredient.Amount;
                if (ingredient.Tag != ItemTag.None) {
                    foreach (Item material in materialsByTag[ingredient.Tag]) {
                        remaining -= material.Amount;
                        if (remaining <= 0) {
                            break;
                        }
                    }
                } else {
                    foreach (Item material in materialsById[ingredient.ItemId]) {
                        remaining -= material.Amount;
                        if (remaining <= 0) {
                            break;
                        }
                    }
                }

                if (remaining > 0) {
                    session.Send(MasteryPacket.Error(MasteryError.s_mastery_error_lack_item));
                    return;
                }
            }

            foreach (MasteryRecipeTable.Ingredient ingredient in entry.RequiredItems) {
                int remaining = ingredient.Amount;
                if (ingredient.Tag != ItemTag.None) {
                    foreach (Item material in materialsByTag[ingredient.Tag]) {
                        int consume = Math.Min(remaining, material.Amount);
                        if (!session.Item.Inventory.Consume(material.Uid, consume)) {
                            Logger.Fatal("Failed to consume item {ItemUid}", material.Uid);
                            throw new InvalidOperationException($"Fatal: Consuming item: {material.Uid}");
                        }

                        remaining -= consume;
                        if (remaining <= 0) {
                            break;
                        }
                    }
                } else {
                    foreach (Item material in materialsById[ingredient.ItemId]) {
                        int consume = Math.Min(remaining, material.Amount);
                        if (!session.Item.Inventory.Consume(material.Uid, consume)) {
                            Logger.Fatal("Failed to consume item {ItemUid}", material.Uid);
                            throw new InvalidOperationException($"Fatal: Consuming item: {material.Uid}");
                        }

                        remaining -= consume;
                        if (remaining <= 0) {
                            break;
                        }
                    }
                }
            }
        }

        session.Currency.Meso -= entry.RequiredMeso;

        if (!entry.NoRewardExp) {
            session.Mastery[entry.Type] += entry.RewardMastery;
        }

        foreach (MasteryRecipeTable.Ingredient rewardItem in entry.RewardItems) {
            if (!ItemMetadata.TryGet(rewardItem.ItemId, out ItemMetadata? itemMetadata)) {
                continue;
            }
            Item item = new(itemMetadata, rewardItem.Rarity, rewardItem.Amount);
            if (!session.Item.Inventory.CanAdd(item)) {
                // TODO: Mail to player
                continue;
            }
            session.Item.Inventory.Add(item, true);
        }
        session.Send(MasteryPacket.GetCraftedItem(entry.Type, (ICollection<MasteryRecipeTable.Ingredient>) entry.RewardItems));

    }
}
