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

namespace Maple2.Server.Game.PacketHandlers;

public class ItemExchangeScroll : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.ItemExchangeScroll;

    private enum Command : byte {
        Exchange = 1,
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
            case Command.Exchange:
                HandleExchange(session, packet);
                break;
        }
    }

    private void HandleExchange(GameSession session, IByteReader packet) {
        long scrollItemUid = packet.ReadLong();
        long unk = packet.ReadLong(); // item uid for upgrading exchange type?
        int quantity = packet.ReadInt();

        Item? scrollItem = session.Item.Inventory.Get(scrollItemUid);

        if (scrollItem == null) {
            session.Send(ItemExchangeScrollPacket.Error(ItemExchangeScrollError.s_itemslot_exchange_scroll_invalid));
            return;
        }
            
        if (!int.TryParse(scrollItem.Metadata.Function?.Parameters, out int scrollId) || !TableMetadata.ItemExchangeScrollTable.Entries.TryGetValue(scrollId, out ItemExchangeScrollMetadata? scrollMetadata)) {
            session.Send(ItemExchangeScrollPacket.Error(ItemExchangeScrollError.s_itemslot_exchange_unknown));
            return;
        }
            
        if (session.Currency.CanAddMeso(-scrollMetadata.RequiredMeso * quantity) != -scrollMetadata.RequiredMeso * quantity) {
            session.Send(ItemExchangeScrollPacket.Error(ItemExchangeScrollError.s_itemslot_exchange_money_invalid));
            return;
        }
        
        lock (session.Item) {
            // Check if player has enough scrolls
            List<Item> scrolls = session.Item.Inventory.Find(scrollMetadata.RecipeScroll.ItemId, scrollMetadata.RecipeScroll.Rarity).ToList();
            if (scrolls.Sum(x => x.Amount) < scrollMetadata.RecipeScroll.Amount * quantity) {
                session.Send(ItemExchangeScrollPacket.Error(ItemExchangeScrollError.s_itemslot_exchange_count_invalid));
                return;
            }
            
            // Check for ingredients
            Dictionary<int, List<Item>> materialsById = scrollMetadata.RequiredItems.ToDictionary(
                ingredient => ingredient.ItemId,
                ingredient => session.Item.Inventory.Find(ingredient.ItemId, ingredient.Rarity).ToList()
            );
            Dictionary<ItemTag, IList<Item>> materialsByTag = scrollMetadata.RequiredItems.ToDictionary(
                ingredient => ingredient.Tag,
                ingredient => session.Item.Inventory.Filter(item => item.Metadata.Property.Tag == ingredient.Tag)
            );
            
            foreach (Ingredient ingredient in scrollMetadata.RequiredItems) {
                int remaining = ingredient.Amount * quantity;
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
                    session.Send(ItemExchangeScrollPacket.Error(ItemExchangeScrollError.s_itemslot_exchange_count_invalid));
                    return;
                }
            }
            
            // Consume scrolls
            int remainingScrolls = scrollMetadata.RecipeScroll.Amount * quantity;
            foreach (Item scroll in scrolls) {
                int consume = Math.Min(remainingScrolls, scroll.Amount);
                if (!session.Item.Inventory.Consume(scroll.Uid, consume)) {
                    Logger.Fatal("Failed to consume item {ItemUid}", scroll.Uid);
                    throw new InvalidOperationException($"Fatal: Consuming item: {scroll.Uid}");
                }
            }

            // Consume ingredients
            foreach (Ingredient ingredient in scrollMetadata.RequiredItems) {
                int remainingIngredients = ingredient.Amount * quantity;
                if (ingredient.Tag != ItemTag.None) {
                    foreach (Item material in materialsByTag[ingredient.Tag]) {
                        int consume = Math.Min(remainingIngredients, material.Amount);
                        if (!session.Item.Inventory.Consume(material.Uid, consume)) {
                            Logger.Fatal("Failed to consume item {ItemUid}", material.Uid);
                            throw new InvalidOperationException($"Fatal: Consuming item: {material.Uid}");
                        }

                        remainingIngredients -= consume;
                        if (remainingIngredients <= 0) {
                            break;
                        }
                    }
                } else {
                    foreach (Item material in materialsById[ingredient.ItemId]) {
                        int consume = Math.Min(remainingIngredients, material.Amount);
                        if (!session.Item.Inventory.Consume(material.Uid, consume)) {
                            Logger.Fatal("Failed to consume item {ItemUid}", material.Uid);
                            throw new InvalidOperationException($"Fatal: Consuming item: {material.Uid}");
                        }

                        remainingIngredients -= consume;
                        if (remainingIngredients <= 0) {
                            break;
                        }
                    }
                }
            }
        }
        
        session.Currency.Meso -= scrollMetadata.RequiredMeso;

        if (!ItemMetadata.TryGet(scrollMetadata.RewardItem.ItemId, out ItemMetadata? itemMetadata)) {
            return;
        }
            
        var rewardItem = new Item(itemMetadata, scrollMetadata.RewardItem.Rarity, scrollMetadata.RewardItem.Amount * quantity);
        if (!session.Item.Inventory.Add(rewardItem, true)) {
            // TODO: mail player
        }
        session.Send(ItemExchangeScrollPacket.Error(ItemExchangeScrollError.s_itemslot_exchange_ok));
    }
}
