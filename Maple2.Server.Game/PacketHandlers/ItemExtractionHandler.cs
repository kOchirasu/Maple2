using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Storage;
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
    public ItemMetadataStorage ItemMetadata { get; init; } = null!;
    public TableMetadataStorage TableMetadata { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        long anvilItemUid = packet.ReadLong();
        long sourceItemUid = packet.ReadLong();

        Item? anvil = session.Item.Inventory.Get(anvilItemUid);
        Item? sourceItem = session.Item.Inventory.Get(sourceItemUid);

        if (anvil == null || sourceItem is not {GlamorForges: > 0}) {
            return;
        }

        if (!TableMetadata.ItemExtractionTable.Entries.TryGetValue(sourceItem.Id, out ItemExtractionTable.Entry? entry)) {
            return;
        }

        IEnumerable<Item> anvils = session.Item.Inventory.Filter(item => item.Metadata.Function?.Type == ItemFunction.ItemExtraction);
        if (anvils.Sum(item => item.Amount) < entry.ScrollCount) {
            session.Send(ItemExtractionPacket.InsufficientAnvils());
            return;
        }

        if (!ItemMetadata.TryGet(entry.ResultItemId, out ItemMetadata? resultItemMetadata)) {
            return;
        }

        Item? resultItem = new(resultItemMetadata);

        if (!session.Item.Inventory.CanAdd(resultItem)) {
            session.Send(ItemExtractionPacket.FullInventory());
            return;
        }

        resultItem = session.GameStorage.Context().CreateItem(0, resultItem);
        session.Item.Inventory.Add(resultItem, true);
        sourceItem.GlamorForges--;

        int remaining = entry.ScrollCount;
        foreach (Item item in anvils) {
            int consume = Math.Min(remaining, item.Amount);
            if (!session.Item.Inventory.Consume(item.Uid, consume)) {
                Logger.Fatal("Failed to consume item {ItemUid}", item.Uid);
                throw new InvalidOperationException($"Fatal: Consuming item: {item.Uid}");
            }
            if (remaining <= 0) {
                break;
            }
        }

        session.Send(ItemExtractionPacket.Extract(sourceItem.Uid, resultItem));
    }
}
