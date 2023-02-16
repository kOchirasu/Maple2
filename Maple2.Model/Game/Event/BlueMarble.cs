using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game.Event;

/// <summary>
/// Mapleopoly
/// </summary>
public class BlueMarble : GameEventInfo {
    public IList<BlueMarbleEntry> Entries { get; set; }
    public IList<BlueMarbleTile> Tiles { get; set; }

    public override void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteInt(Entries.Count);
        foreach (BlueMarbleEntry entry in Entries) {
            writer.Write<BlueMarbleEntry>(entry);
        }
    }
}

public class BlueMarbleTile : IByteSerializable {
    public int Position { get; set; }
    public BlueMarbleTileType Type { get; set; }
    public int MoveAmount { get; set; }
    public BlueMarbleItem? Item { get; set; }
    
    [JsonConstructor]
    public BlueMarbleTile(int position, BlueMarbleTileType type, int moveAmount, BlueMarbleItem? item) {
        Position = position;
        Type = type;
        MoveAmount = moveAmount;
        Item = item;
    }
    
    public void WriteTo(IByteWriter writer) {
        writer.Write<BlueMarbleTileType>(Type);
        writer.WriteInt(MoveAmount);
        writer.Write<BlueMarbleItem>(Item ?? BlueMarbleItem.Default);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 13)]
public readonly record struct BlueMarbleEntry(int TripAmount, BlueMarbleItem Item);

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 9)]
public readonly record struct BlueMarbleItem(int ItemId, byte ItemRarity, int ItemAmount) {
    public static readonly BlueMarbleItem Default = new BlueMarbleItem(0, 0, 0);
}