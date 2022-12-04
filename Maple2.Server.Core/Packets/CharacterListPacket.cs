﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Maple2.Database.Extensions;
using Maple2.Model;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets.Helper;
using Maple2.Tools.Extensions;
using Equips = System.Collections.Generic.IDictionary<Maple2.Model.Enum.ItemGroup,
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
        NameChanged = 7,
    }

    public static ByteWriter AddEntries(Account account, ICollection<(Character, Equips)> entry) {
        var pWriter = Packet.Of(SendOp.CharacterList);
        pWriter.Write<Command>(Command.List);
        pWriter.WriteByte((byte) entry.Count); // CharCount
        foreach ((Character character, Equips equips) in entry) {
            pWriter.WriteEntry(account, character, equips);
        }

        return pWriter;
    }

    // Sent after creating a character to append to list
    public static ByteWriter AppendEntry(Account account, Character character, Equips equips) {
        var pWriter = Packet.Of(SendOp.CharacterList);
        pWriter.Write<Command>(Command.AppendEntry);
        pWriter.WriteEntry(account, character, equips);
        return pWriter;
    }

    public static ByteWriter DeleteEntry(long characterId, CharacterDeleteError error = default) {
        var pWriter = Packet.Of(SendOp.CharacterList);
        pWriter.Write<Command>(Command.DeleteEntry);
        pWriter.Write<CharacterDeleteError>(error);
        pWriter.WriteLong(characterId);
        return pWriter;
    }

    public static ByteWriter BeginDelete(long characterId, long deleteTime, CharacterDeleteError error = default) {
        var pWriter = Packet.Of(SendOp.CharacterList);
        pWriter.Write<Command>(Command.BeginDelete);
        pWriter.WriteLong(characterId);
        pWriter.Write<CharacterDeleteError>(error);
        pWriter.WriteLong(deleteTime);
        return pWriter;
    }

    public static ByteWriter CancelDelete(long characterId, CharacterDeleteError error = default) {
        var pWriter = Packet.Of(SendOp.CharacterList);
        pWriter.Write<Command>(Command.CancelDelete);
        pWriter.WriteLong(characterId);
        pWriter.Write<CharacterDeleteError>(error);
        return pWriter;
    }
    
    public static ByteWriter NameChanged(long characterId, string characterName) {
        var pWriter = Packet.Of(SendOp.CharacterList);
        pWriter.Write<Command>(Command.NameChanged);
        pWriter.WriteInt(1); // unknown
        pWriter.WriteLong(characterId);
        pWriter.WriteUnicodeString(characterName);
        return pWriter;
    }

    public static ByteWriter StartList() {
        var pWriter = Packet.Of(SendOp.CharacterList);
        pWriter.Write<Command>(Command.StartList);
        return pWriter;
    }

    // This only needs to be sent if char count > 0
    public static ByteWriter EndList() {
        var pWriter = Packet.Of(SendOp.CharacterList);
        pWriter.Write<Command>(Command.EndList);
        pWriter.WriteBool(false);
        return pWriter;
    }

    public static ByteWriter CreateError(CharacterCreateError error, string message = "") {
        var pWriter = Packet.Of(SendOp.CharacterCreate);
        pWriter.Write<CharacterCreateError>(error);
        pWriter.WriteUnicodeString(message);

        return pWriter;
    }

    public static ByteWriter SetMax(int unlocked, int total) {
        var pWriter = Packet.Of(SendOp.CharMaxCount);
        pWriter.WriteInt(unlocked);
        pWriter.WriteInt(total);
        return pWriter;
    }

    private static void WriteEntry(this IByteWriter pWriter, Account account, Character character, Equips equips) {
        pWriter.WriteCharacter(account, character);
        pWriter.WriteUnicodeString(character.Picture);
        pWriter.WriteLong(character.DeleteTime);

        equips.TryGetValue(ItemGroup.Gear, out List<Item>? gears);
        gears ??= new List<Item>();
        equips.TryGetValue(ItemGroup.Outfit, out List<Item>? outfits);
        outfits ??= new List<Item>();
        pWriter.WriteByte((byte) (gears.Count + outfits.Count));
        foreach (Item gear in gears) {
            pWriter.WriteEquip(gear);
        }
        foreach (Item outfit in outfits) {
            pWriter.WriteEquip(outfit);
        }

        equips.TryGetValue(ItemGroup.Badge, out List<Item>? badges);
        badges ??= new List<Item>();
        pWriter.WriteByte((byte) badges.Count);
        foreach (Item badge in badges) {
            pWriter.WriteBadge(badge);
        }

        pWriter.WriteByte(); // Outfit2
    }

    private static void WriteCharacter(this IByteWriter writer, Account account, Character character) {
        writer.WriteLong(account.Id);
        writer.WriteLong(character.Id);
        writer.WriteUnicodeString(character.Name);
        writer.Write<Gender>(character.Gender);
        writer.WriteByte(1);
        writer.WriteLong();
        writer.WriteInt();
        writer.WriteInt(character.MapId);
        writer.WriteInt(character.InstanceMapId);
        writer.WriteInt(character.InstanceId);
        writer.WriteShort(character.Level);
        writer.WriteShort(character.Channel);
        writer.WriteInt((int)character.Job.Code());
        writer.Write<Job>(character.Job);
        writer.WriteInt(); // CurrentHp
        writer.WriteInt(); // MaxHp
        writer.WriteShort();
        writer.WriteLong();
        writer.WriteLong(character.StorageCooldown);
        writer.WriteLong(character.DoctorCooldown);
        writer.WriteInt(character.ReturnMapId);
        writer.Write<Vector3>(character.ReturnPosition);
        writer.WriteInt(); // GearScore
        writer.Write<SkinColor>(character.SkinColor);
        writer.WriteLong(character.CreationTime);
        writer.Write<Trophy>(account.Trophy);
        writer.WriteLong(character.GuildId);
        writer.WriteUnicodeString(character.GuildName);
        writer.WriteUnicodeString(character.Motto);
        writer.WriteUnicodeString(character.Picture);
        writer.WriteByte(); // Club Count
        writer.WriteByte(); // PCBang related?
        writer.WriteClass<Mastery>(character.Mastery);
        #region Unknown
        writer.WriteUnicodeString();
        writer.WriteLong();
        writer.WriteLong();
        writer.WriteLong();
        #endregion
        writer.WriteInt(); // Unknown Count
        writer.WriteByte();
        writer.WriteBool(false);
        writer.WriteLong(); // Birthday
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        writer.WriteInt(account.PrestigeLevel); // PrestigeLevel
        writer.WriteLong(character.LastModified.ToEpochSeconds());
        writer.WriteInt(); // Unknown Count
        writer.WriteInt(); // Unknown Count
        writer.WriteShort(); // Survival related?
        writer.WriteLong();
    }
}
