﻿using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class ItemExtractionHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.ItemExtractionScroll;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public ItemMetadataStorage ItemMetadata { private get; init; } = null!;
    public TableMetadataStorage TableMetadata { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        long anvilItemUid = packet.ReadLong();
        long sourceItemUid = packet.ReadLong();

        lock (session.Item) {
            Item? anvil = session.Item.Inventory.Get(anvilItemUid);
            Item? sourceItem = session.Item.Inventory.Get(sourceItemUid);

            if (anvil == null || sourceItem is not {GlamorForges: > 0}) {
                return;
            }

            if (!TableMetadata.ItemExtractionTable.Entries.TryGetValue(sourceItem.Id, out ItemExtractionTable.Entry? entry)) {
                return;
            }

            if (!ItemMetadata.TryGet(entry.ResultItemId, out ItemMetadata? resultItemMetadata)) {
                return;
            }

            if (session.Item.Inventory.FreeSlots(InventoryType.Gear) <= 0) {
                session.Send(ItemExtractionPacket.FullInventory());
                return;
            }

            var anvils = new IngredientInfo(ItemTag.ItemExtraction, entry.ScrollCount);
            if (!session.Item.Inventory.Consume(new []{anvils})) {
                session.Send(ItemExtractionPacket.InsufficientAnvils());
                return;
            }

            using GameStorage.Request db = session.GameStorage.Context();
            Item? resultItem = db.CreateItem(0, new Item(resultItemMetadata));
            session.Item.Inventory.Add(resultItem, true);
            sourceItem.GlamorForges--;

            session.Send(ItemExtractionPacket.Extract(sourceItem.Uid, resultItem));
        }
    }
}
