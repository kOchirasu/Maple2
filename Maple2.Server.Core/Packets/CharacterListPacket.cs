using System;
using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets.Helper;
using Maple2.Tools.Extensions;
using Equips = System.Collections.Generic.IDictionary<Maple2.Model.Enum.EquipTab,
    System.Collections.Generic.List<Maple2.Model.Game.Item>>;

namespace Maple2.Server.Core.Packets;

public static class CharacterListPacket {
    private enum Command : byte {
        List = 0,
        AppendEntry = 1,
        DeleteEntry = 2,
        StartList = 3,
        EndList = 4,
        BeginDelete = 5,
        CancelDelete = 6,
    }

    public static ByteWriter AddEntries(Account account, ICollection<(Character, Equips)> entry) {
        var pWriter = Packet.Of(SendOp.CHARACTER_LIST);
        pWriter.Write<Command>(Command.List);
        pWriter.WriteByte((byte) entry.Count); // CharCount
        foreach ((Character character, Equips equips) in entry) {
            pWriter.WriteEntry(account, character, equips);
        }

        return pWriter;
    }

    // Sent after creating a character to append to list
    public static ByteWriter AppendEntry(Account account, Character character, Equips equips) {
        var pWriter = Packet.Of(SendOp.CHARACTER_LIST);
        pWriter.Write<Command>(Command.AppendEntry);
        pWriter.WriteEntry(account, character, equips);
        return pWriter;
    }

    public static ByteWriter DeleteEntry(long characterId, CharacterDeleteError error = default) {
        var pWriter = Packet.Of(SendOp.CHARACTER_LIST);
        pWriter.Write<Command>(Command.DeleteEntry);
        pWriter.Write<CharacterDeleteError>(error);
        pWriter.WriteLong(characterId);
        return pWriter;
    }

    public static ByteWriter BeginDelete(long characterId, long deleteTime, CharacterDeleteError error = default) {
        var pWriter = Packet.Of(SendOp.CHARACTER_LIST);
        pWriter.Write<Command>(Command.BeginDelete);
        pWriter.WriteLong(characterId);
        pWriter.Write<CharacterDeleteError>(error);
        pWriter.WriteLong(deleteTime);
        return pWriter;
    }

    public static ByteWriter CancelDelete(long characterId, CharacterDeleteError error = default) {
        var pWriter = Packet.Of(SendOp.CHARACTER_LIST);
        pWriter.Write<Command>(Command.CancelDelete);
        pWriter.WriteLong(characterId);
        pWriter.Write<CharacterDeleteError>(error);
        return pWriter;
    }

    public static ByteWriter SetMax(int unlocked, int total) {
        var pWriter = Packet.Of(SendOp.CHAR_MAX_COUNT);
        pWriter.WriteInt(unlocked);
        pWriter.WriteInt(total);
        return pWriter;
    }

    public static ByteWriter StartList() {
        var pWriter = Packet.Of(SendOp.CHARACTER_LIST);
        pWriter.Write<Command>(Command.StartList);
        return pWriter;
    }

    // This only needs to be sent if char count > 0
    public static ByteWriter EndList() {
        var pWriter = Packet.Of(SendOp.CHARACTER_LIST);
        pWriter.Write<Command>(Command.EndList);
        pWriter.WriteBool(false);
        return pWriter;
    }

    private static void WriteEntry(this IByteWriter pWriter, Account account, Character character, Equips equips) {
        pWriter.WriteCharacter(account, character);
        pWriter.WriteUnicodeString(character.Picture);
        pWriter.WriteLong(character.DeleteTime);

        equips.TryGetValue(EquipTab.Gear, out List<Item>? gears);
        gears ??= new List<Item>();
        equips.TryGetValue(EquipTab.Outfit, out List<Item>? outfits);
        outfits ??= new List<Item>();
        pWriter.WriteByte((byte) (gears.Count + outfits.Count));
        foreach (Item gear in gears) {
            pWriter.WriteInt(gear.Id);
            pWriter.WriteLong(gear.Uid);
            pWriter.WriteUnicodeString(gear.EquipSlot.ToString());
            pWriter.WriteInt(gear.Rarity);
            pWriter.WriteClass<Item>(gear);
        }

        foreach (Item outfit in outfits) {
            pWriter.WriteInt(outfit.Id);
            pWriter.WriteLong(outfit.Uid);
            pWriter.WriteUnicodeString(outfit.EquipSlot.ToString());
            pWriter.WriteInt(outfit.Rarity);
            pWriter.WriteClass<Item>(outfit);
        }

        equips.TryGetValue(EquipTab.Badge, out List<Item>? badges);
        badges ??= new List<Item>();
        pWriter.WriteByte((byte) badges.Count);
        foreach (Item badge in badges) {
            if (badge.Badge == null) {
                throw new ArgumentNullException(nameof(badge.Badge));
            }

            pWriter.Write<BadgeType>(badge.Badge.Type);
            pWriter.WriteInt(badge.Id);
            pWriter.WriteLong(badge.Uid);
            pWriter.WriteInt(badge.Rarity);
            pWriter.WriteClass<Item>(badge);
        }

        pWriter.WriteByte(); // Outfit2
    }
}
