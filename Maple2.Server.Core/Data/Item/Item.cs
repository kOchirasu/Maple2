using System;
using Maple2.Database.Model.Metadata;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Core.Data; 

public class Item : IByteSerializable {
    public readonly ItemMetadata Metadata;
    public readonly InventoryType Inventory;

    public readonly int Id;
    public readonly long Uid;
    public int Rarity;
    
    public short Slot;
    public int Amount;

    public long CreationTime;
    public long ExpiryTime;

    public int TimeChangedOption;
    public int RemainUses;
    public bool IsLocked;
    public long UnlockTime;
    public short GlamorForges;

    public ItemAppearance Appearance;
    public ItemStats Stats;
    public ItemEnchant Enchant;
    public ItemLimitBreak LimitBreak;

    public ItemTransfer Transfer;
    public ItemSocket Socket;
    public ItemCoupleInfo CoupleInfo;
    public ItemBinding Binding;

    #region Special Types
    public UgcItemLook? Template;
    public ItemBlueprint? Blueprint;
    public ItemPet? Pet;
    public ItemCustomMusicScore? Music;
    public ItemBadge? Badge;
    #endregion

    public Item(ItemMetadata metadata, long uid, int rarity) {
        Metadata = metadata;
        Inventory = Metadata.Property.Type switch {
            0 => Metadata.Property.SubType == 2 ? InventoryType.Consumable : InventoryType.Misc, // Unknown
            1 => Metadata.Property.IsSkin ? InventoryType.Outfit : InventoryType.Gear,
            2 => Metadata.Property.SubType == 2 ? InventoryType.Consumable : InventoryType.Misc,
            3 => InventoryType.Quest,
            4 => Metadata.Property.SubType == 2 ? InventoryType.Consumable : InventoryType.Misc,
            5 => InventoryType.Mount, // Air mount
            6 => InventoryType.FishingMusic, // Furnishing shows up in FishingMusic
            7 => InventoryType.Badge,
            9 => InventoryType.Mount, // Ground mount
            10 => Metadata.Property.SubType != 20 ? InventoryType.Misc : InventoryType.FishingMusic,
            11 => InventoryType.Pets,
            12 => InventoryType.FishingMusic, // Music Score
            13 => InventoryType.Gemstone,
            14 => InventoryType.Gemstone, // Gem dust
            15 => InventoryType.Catalyst,
            16 => InventoryType.LifeSkill,
            17 => throw new ArgumentException(
                $"Invalid Type: {Metadata.Property.Type},{Metadata.Property.SubType}"), // Tab 8
            18 => InventoryType.Consumable,
            19 => InventoryType.Catalyst,
            20 => InventoryType.Currency,
            21 => InventoryType.Lapenshard,
            22 => InventoryType.Misc, // Blueprint
            _ => throw new ArgumentException(
                $"Unknown Tab for: {Metadata.Property.Type},{Metadata.Property.SubType}")
        };
        
        Uid = uid;
        Id = metadata.Id;
        Rarity = rarity;
        
        // Template? or Blueprint
        if (Metadata.Property.SkinType == 99 || Metadata.Property.Type == 22) {
            Template = new UgcItemLook();
            Blueprint = new ItemBlueprint();
        }
        if (Inventory == InventoryType.Pets) {
            Pet = new ItemPet();
        }
        // From IDA, this exists for all type 12.
        if (Id / 100000 == 351) { // 350 is also score, but doesn't have extra data?
            Music = new ItemCustomMusicScore();
        }
        if (Inventory == InventoryType.Badge) {
            Badge = new ItemBadge(Id);
        }
    }
    
    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Amount);
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteLong(CreationTime);
        writer.WriteLong(ExpiryTime);
        writer.WriteLong();
        writer.WriteInt(TimeChangedOption);
        writer.WriteInt(RemainUses);
        writer.WriteBool(IsLocked);
        writer.WriteLong(UnlockTime);
        writer.WriteShort(GlamorForges);
        writer.WriteBool(false);
        writer.WriteInt();

        writer.WriteClass<ItemAppearance>(Appearance);
        writer.WriteClass<ItemStats>(Stats);
        writer.WriteClass<ItemEnchant>(Enchant);
        writer.WriteClass<ItemLimitBreak>(LimitBreak);
        
        if (Template != null && Blueprint != null) {
            writer.WriteClass<UgcItemLook>(Template);
            writer.WriteClass<ItemBlueprint>(Blueprint);
        }
        if (Pet != null) {
            writer.WriteClass<ItemPet>(Pet);
        }
        if (Music != null) {
            writer.WriteClass<ItemCustomMusicScore>(Music);
        }
        if (Badge != null) {
            writer.WriteClass<ItemBadge>(Badge);
        }
        
        writer.WriteClass<ItemTransfer>(Transfer);
        writer.WriteClass<ItemSocket>(Socket);
        writer.WriteClass<ItemCoupleInfo>(CoupleInfo);
        writer.WriteClass<ItemBinding>(Binding);
    }

    public void ReadFrom(IByteReader reader) {
        Amount = reader.ReadInt();
        reader.ReadInt();
        reader.ReadInt();
        CreationTime = reader.ReadLong();
        ExpiryTime = reader.ReadLong();
        reader.ReadLong();
        TimeChangedOption = reader.ReadInt();
        RemainUses = reader.ReadInt();
        IsLocked = reader.ReadBool();
        UnlockTime = reader.ReadLong();
        GlamorForges = reader.ReadShort();
        reader.ReadBool();
        reader.ReadInt();

        Appearance = reader.ReadClass<ItemAppearance>();
        Stats = reader.ReadClass<ItemStats>();
        Enchant = reader.ReadClass<ItemEnchant>();
        LimitBreak = reader.ReadClass<ItemLimitBreak>();
        
        if (Template != null && Blueprint != null) {
            Template = reader.ReadClass<UgcItemLook>();
            Blueprint = reader.ReadClass<ItemBlueprint>();
        }
        if (Pet != null) {
            Pet = reader.ReadClass<ItemPet>();
        }
        if (Music != null) {
            Music = reader.ReadClass<ItemCustomMusicScore>();
        }
        if (Badge != null) {
            Badge = reader.ReadClass<ItemBadge>();
        }
        
        Transfer = reader.ReadClass<ItemTransfer>();
        Socket = reader.ReadClass<ItemSocket>();
        CoupleInfo = reader.ReadClass<ItemCoupleInfo>();
        Binding = reader.ReadClass<ItemBinding>();
    }
}
