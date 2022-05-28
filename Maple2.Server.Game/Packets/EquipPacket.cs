using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class EquipPacket {
    public static ByteWriter EquipItem(IActor<Player> player, Item item, byte type) {
        var pWriter = Packet.Of(SendOp.ITEM_PUT_ON);
        pWriter.WriteInt(player.ObjectId);
        pWriter.WriteInt(item.Id);
        pWriter.WriteLong(item.Uid);
        pWriter.WriteUnicodeString(item.EquipSlot.ToString());
        pWriter.WriteInt(item.Rarity);
        pWriter.WriteByte(type); // 0, 1, 2 (rest invalid)
        pWriter.WriteClass<Item>(item);

        return pWriter;
    }

    public static ByteWriter UnequipItem(IActor<Player> player, Item item) {
        var pWriter = Packet.Of(SendOp.ITEM_PUT_OFF);
        pWriter.WriteInt(player.ObjectId);
        pWriter.WriteLong(item.Uid);

        return pWriter;
    }
}
