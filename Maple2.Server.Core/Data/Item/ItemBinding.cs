using System;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Server.Core.Data; 

public class ItemBinding : IByteSerializable {
    public long CharacterId { get; private set; }
    public string Name { get; private set; }

    public ItemBinding() {
        CharacterId = 0;
        Name = string.Empty;
    }

    public ItemBinding(long characterId, string name) {
        CharacterId = characterId;
        Name = name;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteLong(CharacterId);
        writer.WriteUnicodeString(Name);
    }

    public void ReadFrom(IByteReader reader) {
        CharacterId = reader.ReadLong();
        Name = reader.ReadUnicodeString();
    }

    public override bool Equals(object obj) {
        if (ReferenceEquals(this, obj)) return true;
        if (!(obj is ItemBinding other)) return false;
        return CharacterId == other.CharacterId && Name == other.Name;
    }

    public override int GetHashCode() {
        return HashCode.Combine(CharacterId, Name);
    }
}
