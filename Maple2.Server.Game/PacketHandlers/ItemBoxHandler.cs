using System;
using System.Linq;
using Maple2.Database.Extensions;
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

public class ItemBoxHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestItemBox;

    public override void Handle(GameSession session, IByteReader packet) {
        int itemId = packet.ReadInt();
        short unk = packet.ReadShort();
        int count = packet.ReadInt();
        short unk2 = packet.ReadShort(); // is 1 on select item box, 0 on open item box
        ItemBoxError error = ItemBoxError.ok;
        
        Item? item = session.Item.Inventory.Find(itemId).FirstOrDefault();
        switch (item?.Metadata.Function?.Type) {
            case ItemFunction.SelectItemBox:
                int index = packet.ReadShort() - 48;
                error = session.ItemDrop.HandleSelectItemBox(index, item, count);
                break;
            case ItemFunction.OpenItemBox:
                error = session.ItemDrop.HandleOpenItemBox(item, count);
                break;
            default:
                throw new ArgumentOutOfRangeException(item?.Metadata.Function?.Type.ToString(), "Invalid item function type");
        }
        session.Send(ItemBoxPacket.Open(itemId, session.ItemDrop.BoxCount, error));
        session.ItemDrop.Reset();
    }
}
