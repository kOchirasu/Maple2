﻿using System.Collections.Generic;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class ItemScriptPacket {
    private enum Command : byte {
        TreasureChest = 4,
        Gacha = 5,
    }

    public static ByteWriter TreasureChest(int itemId) {
        var pWriter = Packet.Of(SendOp.ItemScript);
        pWriter.Write<Command>(Command.TreasureChest);
        pWriter.WriteInt(itemId);

        return pWriter;
    }

    public static ByteWriter Gacha(ICollection<Item> items) {
        var pWriter = Packet.Of(SendOp.ItemScript);
        pWriter.Write<Command>(Command.Gacha);
        pWriter.WriteInt(items.Count);
        foreach (Item item in items) {
            pWriter.WriteInt(item.Id);
            pWriter.WriteInt(item.Amount);
            pWriter.WriteInt(item.Rarity);
            pWriter.WriteByte();
        }

        return pWriter;
    }
}
