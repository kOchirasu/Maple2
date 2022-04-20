using System;
using System.Collections.Generic;
using System.Diagnostics;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;

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
    
    public static ByteWriter AddEntries(ICollection<Character> characters) {
        var pWriter = Packet.Of(SendOp.CHARACTER_LIST);
        pWriter.Write<Command>(Command.List);
        pWriter.WriteByte((byte)characters.Count); // CharCount
        foreach (Character character in characters) {
            pWriter.WriteEntry(character);
        }

        return pWriter;
    }

    // Sent after creating a character to append to list
    public static ByteWriter AppendEntry(Character character) {
        var pWriter = Packet.Of(SendOp.CHARACTER_LIST);
        pWriter.Write<Command>(Command.AppendEntry);
        pWriter.WriteEntry(character);
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
    
    private static void WriteEntry(this IByteWriter pWriter, Character character) {
        /*pWriter.WriteCharacter(character);
        pWriter.WriteUnicodeString(character.Character.DisplayPicture);
        pWriter.WriteLong(); // Deletion timer

        pWriter.WriteByte((byte) (character.GearEquip.Count + character.OutfitEquip.Count));
        foreach (Item gear in character.GearEquip) {
            pWriter.WriteEquip(gear);
        }
        foreach (Item outfit in character.OutfitEquip) {
            pWriter.WriteEquip(outfit);
        }

        pWriter.WriteByte((byte) character.BadgeEquip.Count);
        foreach (Item badge in character.BadgeEquip) {
            pWriter.WriteBadge(badge as BadgeItem);
        }

        var boolValue = false;
        pWriter.WriteBool(boolValue);
        if (boolValue) {
            pWriter.WriteLong();
            pWriter.WriteLong();
            var otherBoolValue = true;
            pWriter.WriteBool(otherBoolValue);
            if (otherBoolValue) {
                pWriter.WriteInt();
                pWriter.WriteLong();
                pWriter.WriteUnicodeString("abc");
                pWriter.WriteInt();
            }
        }*/
    }
}
