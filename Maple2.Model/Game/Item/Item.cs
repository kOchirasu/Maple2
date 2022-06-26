using System;
using System.Linq;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game;

public class Item : IByteSerializable, IByteDeserializable {
    public readonly ItemMetadata Metadata;
    public readonly InventoryType Inventory;

    public DateTime LastModified { get; init; }

    public long Uid { get; init; } = -1;
    public int Rarity { get; init; }
    public short Slot = -1;
    public EquipTab EquipTab = EquipTab.None;
    public EquipSlot EquipSlot = EquipSlot.Unknown;

    public int Id => Metadata.Id;
    public int Amount;

    public long CreationTime;
    public long ExpiryTime;

    public int TimeChangedOption;
    public int RemainUses;
    public bool IsLocked;
    public long UnlockTime;
    public short GlamorForges;

    public ItemAppearance? Appearance;
    public ItemStats? Stats;
    public ItemEnchant? Enchant;
    public ItemLimitBreak? LimitBreak;

    public ItemTransfer? Transfer;
    public ItemSocket? Socket;
    public ItemCoupleInfo? CoupleInfo;
    public ItemBinding? Binding;

    #region Special Types
    public UgcItemLook? Template;
    public ItemBlueprint? Blueprint;
    public ItemPet? Pet;
    public ItemCustomMusicScore? Music;
    public ItemBadge? Badge;
    #endregion

    public Item(ItemMetadata metadata, int rarity = 1, int amount = 1, bool initialize = true) {
        Metadata = metadata;
        Rarity = rarity;
        Amount = amount;
        Inventory = Metadata.Inventory();

        // Skip initialization of fields, this is done if we will initialize separately.
        if (!initialize) {
            return;
        }

        Appearance = Metadata.SlotNames.FirstOrDefault(EquipSlot.Unknown) switch {
            EquipSlot.HR => new HairAppearance(default),
            EquipSlot.FD => new DecalAppearance(default),
            EquipSlot.CP => new CapAppearance(default),
            _ => new ItemAppearance(default),
        };

        Transfer = new ItemTransfer(GetTransferFlag(), Metadata.Property.TradableCount, Metadata.Property.RepackCount);

        // Template? or Blueprint
        if (Metadata.Mesh != string.Empty || Metadata.Property.Type == 22) {
            Template = new UgcItemLook();
            Blueprint = new ItemBlueprint();
        } else if (Inventory == InventoryType.Pets) {
            Pet = new ItemPet();
        } else if (this.IsCustomMusicScore()) { // 350 is also score, but doesn't have extra data?
            // From IDA, this exists for all type 12.
            Music = new ItemCustomMusicScore();
        } else if (Inventory == InventoryType.Badge) {
            Badge = new ItemBadge(Id);
        }
    }

    private TransferFlag GetTransferFlag() {
        bool zeroTrades = Metadata.Property.TradableCount <= 0;
        bool belowRarity = Rarity < Metadata.Limit.TradeMaxRarity;
        switch (Metadata.Limit.TransferType) {
            case 0: // Tradeable
                if (belowRarity) {
                    return TransferFlag.Trade | TransferFlag.Split;
                }
                return zeroTrades ? TransferFlag.None : TransferFlag.LimitTrade;
            case 1: // Untradeable
                return zeroTrades ? TransferFlag.None : TransferFlag.LimitTrade;
            case 2: // Binds on loot
            case 3: // Binds on equip
            case 4: // Binds on use
            case 5: // Binds on trade
            case 7: // Binds pet (summon/enchant/reroll)
                var result = TransferFlag.Bind;
                if (zeroTrades) {
                    if (belowRarity) {
                        result &= TransferFlag.Trade | TransferFlag.Split;
                    }
                } else {
                    result &= TransferFlag.LimitTrade;
                }
                return result;
            case 6:
                if (!zeroTrades || belowRarity) {
                    return TransferFlag.Trade;
                }
                return zeroTrades ? TransferFlag.None : TransferFlag.LimitTrade;
            default:
                return TransferFlag.None;
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

        writer.WriteClass<ItemAppearance>(Appearance ?? ItemAppearance.Default);
        writer.WriteClass<ItemStats>(Stats ?? ItemStats.Default);
        writer.WriteClass<ItemEnchant>(Enchant ?? ItemEnchant.Default);
        writer.WriteClass<ItemLimitBreak>(LimitBreak ?? ItemLimitBreak.Default);

        if (Template != null && Blueprint != null) {
            writer.WriteClass<UgcItemLook>(Template);
            writer.WriteClass<ItemBlueprint>(Blueprint);
        } else if (Pet != null) {
            writer.WriteClass<ItemPet>(Pet);
        } else if (Music != null) {
            writer.WriteClass<ItemCustomMusicScore>(Music);
        } else if (Badge != null) {
            writer.WriteClass<ItemBadge>(Badge);
        }

        writer.WriteClass<ItemTransfer>(Transfer ?? ItemTransfer.Default);
        writer.WriteClass<ItemSocket>(Socket ?? ItemSocket.Default);
        writer.WriteClass<ItemCoupleInfo>(CoupleInfo ?? ItemCoupleInfo.Default);
        writer.WriteClass<ItemBinding>(Binding ?? ItemBinding.Default);
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
        } else if (Pet != null) {
            Pet = reader.ReadClass<ItemPet>();
        } else if (Music != null) {
            Music = reader.ReadClass<ItemCustomMusicScore>();
        } else if (Badge != null) {
            Badge = reader.ReadClass<ItemBadge>();
        }

        Transfer = reader.ReadClass<ItemTransfer>();
        Socket = reader.ReadClass<ItemSocket>();
        CoupleInfo = reader.ReadClass<ItemCoupleInfo>();
        Binding = reader.ReadClass<ItemBinding>();
    }
}
