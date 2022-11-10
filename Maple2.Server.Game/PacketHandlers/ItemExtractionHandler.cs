using System;
using Maple2.Database.Extensions;
using Maple2.Database.Storage;
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
    public TableMetadataStorage TableMetadata { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        long anvilItemUid = packet.ReadLong();
        long sourceItemUid = packet.ReadLong();

        Item? anvil = session.Item.Inventory.Get(anvilItemUid);
        Item? sourceItem = session.Item.Inventory.Get(sourceItemUid);

        if (anvil == null || sourceItem == null) {
            return;
        }

        if (!TableMetadata.ItemExtractionTable.Entries.TryGetValue(sourceItem.Id, out ItemExtractionTable.Entry? entry)) {
            return;
        }


    }
}
