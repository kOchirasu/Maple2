using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class MasteryHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Mastery;

    private enum Command : byte {
        Reward = 1,
        Craft = 2,
    }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public ItemMetadataStorage ItemMetadata { get; init; } = null!;
    public TableMetadataStorage TableMetadata { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Craft:
                HandleCraft(session, packet);
                break;
        }
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

        var itemDictionary = new Dictionary<int, IEnumerable<Item>>();
        foreach (MasteryRecipeTable.Ingredient ingredient in entry.RequiredItems) {
            IEnumerable<Item> foundItems = session.Item.Inventory.Find(ingredient.ItemId, ingredient.Rarity).ToList();
            int totalCount = foundItems.Sum(item => item.Amount);
            if (totalCount < ingredient.Amount) {
                session.Send(MasteryPacket.Error(MasteryError.s_mastery_error_lack_item));
                return;
            }
            itemDictionary.Add(ingredient.ItemId, foundItems);
        }

        if (entry.RequiredItems.Any(ingredient => session.Item.Inventory.Find(ingredient.ItemId, ingredient.Rarity).ToList().Count < ingredient.Amount)) {
            session.Send(MasteryPacket.Error(MasteryError.s_mastery_error_lack_item));
            return;
        }

        session.Currency.Meso -= entry.RequiredMeso;

        if (!entry.NoRewardExp) {
            session.Mastery[entry.Type] += entry.RewardMastery;
            session.Send(MasteryPacket.UpdateMastery(entry.Type, session.Mastery[entry.Type]));
        }

        foreach (MasteryRecipeTable.Ingredient ingredient in entry.RequiredItems) {
            IEnumerable<Item> itemList = itemDictionary[ingredient.ItemId];
            int amountToConsume = ingredient.Amount;
            foreach (Item item in itemList) {
                if (amountToConsume == 0) {
                    break;
                }
                if (item.Amount >= amountToConsume) {
                    session.Item.Inventory.Consume(item.Uid, amountToConsume);
                    break;
                }

                session.Item.Inventory.Consume(item.Uid, item.Amount);
                amountToConsume -= item.Amount;
            }
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
            session.Send(MasteryPacket.GetCraftedItem(entry.Type, rewardItem));
        }
    }
}
