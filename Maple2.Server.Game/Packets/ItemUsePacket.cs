﻿using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class ItemUsePacket {
    private enum Command : byte {
        ExpandInventory = 0,
        MaxInventory = 1,
        CharacterSlotAdded = 2,
        MaxCharacterSlots = 3,
        BeautyCoupon = 6,
    }

    // s_msg_expand_character_slot_complete
    // - A character slot has been added.
    public static ByteWriter CharacterSlotAdded() {
        var pWriter = Packet.Of(SendOp.ItemUse);
        pWriter.Write<Command>(Command.CharacterSlotAdded);

        return pWriter;
    }

    // s_msg_expand_character_slot_already_maximum
    // - You have already unlocked the maximum number of character slots.
    public static ByteWriter MaxCharacterSlots() {
        var pWriter = Packet.Of(SendOp.ItemUse);
        pWriter.Write<Command>(Command.MaxCharacterSlots);

        return pWriter;
    }

    public static ByteWriter BeautyCoupon(int playerObjectId, long itemUid) {
        var pWriter = Packet.Of(SendOp.Emote);
        pWriter.Write<Command>(Command.BeautyCoupon);
        pWriter.WriteInt(playerObjectId);
        pWriter.WriteLong(itemUid);

        return pWriter;
    }
}
