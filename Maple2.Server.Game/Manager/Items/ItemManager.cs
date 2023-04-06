using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Server.Game.Util;

namespace Maple2.Server.Game.Manager.Items;

public class ItemManager {
    private readonly GameSession session;
    private readonly ItemStatsCalculator itemStatsCalc;

    public readonly EquipManager Equips;
    public readonly InventoryManager Inventory;
    public readonly FurnishingManager Furnishing;

    public ItemManager(GameStorage.Request db, GameSession session, ItemStatsCalculator itemStatsCalc) {
        this.session = session;
        this.itemStatsCalc = itemStatsCalc;

        Equips = new EquipManager(db, session);
        Inventory = new InventoryManager(db, session);
        Furnishing = new FurnishingManager(db, session);
    }

    /// <summary>
    /// Retrieves an gear from inventory or equipment.
    /// </summary>
    /// <param name="uid">Uid of the gear to retrieve</param>
    /// <returns>Item if it exists</returns>
    public Item? GetGear(long uid) {
        Item? item = Inventory.Get(uid, InventoryType.Gear);
        return item ?? Equips.Gear.Values.FirstOrDefault(gear => gear.Uid == uid);
    }

    /// <summary>
    /// Retrieves an outfit from inventory or equipment.
    /// </summary>
    /// <param name="uid">Uid of the outfit to retrieve</param>
    /// <returns>Item if it exists</returns>
    public Item? GetOutfit(long uid) {
        Item? item = Inventory.Get(uid, InventoryType.Outfit);
        return item ?? Equips.Outfit.Values.FirstOrDefault(outfit => outfit.Uid == uid);
    }

    public Item CreateItem(ItemMetadata itemMetadata, int rarity = 1, int amount = 1, bool initialize = true) {
        var item = new Item(itemMetadata, rarity, amount, initialize);
        item.Stats = itemStatsCalc.GetStats(item);
        item.Socket = itemStatsCalc.GetSockets(item);
        
        
        item.Appearance = itemMetadata.SlotNames.FirstOrDefault(EquipSlot.Unknown) switch {
            EquipSlot.HR => new HairAppearance(GetColor(item)),
            EquipSlot.FD => new DecalAppearance(GetColor(item)),
            EquipSlot.CP => new CapAppearance(GetColor(item)),
            _ => new ItemAppearance(GetColor(item)),
        };
        
        return item;
    }
    
    private EquipColor GetColor(Item item) {
        // Item has no color
        if (item.Metadata.Customize.ColorPalette == 0 || 
            !session.TableMetadata.ColorPaletteTable.Entries.TryGetValue(item.Metadata.Customize.ColorPalette, out Dictionary<int, ColorPaletteTable.Entry>? palette)) {
            return default;
        }
        
        // Item has random color
        if (item.Metadata.Customize.DefaultColorIndex < 0) {
            // random entry from palette
            int index = Random.Shared.Next(palette.Count);
            ColorPaletteTable.Entry randomEntry = palette.Values.ElementAt(index);
            return new EquipColor(randomEntry.Primary, randomEntry.Secondary, randomEntry.Tertiary, item.Metadata.Customize.ColorPalette, index);
        }
        
        // Item has specified color
        if (palette.TryGetValue(item.Metadata.Customize.DefaultColorIndex, out ColorPaletteTable.Entry? entry)) {
            return new EquipColor(entry.Primary, entry.Secondary, entry.Tertiary, item.Metadata.Customize.ColorPalette, item.Metadata.Customize.DefaultColorIndex);
        }

        return default;
    }

    public void Bind(Item item) {
        if (item.Transfer?.Bind(session.Player.Value.Character) == true) {
            session.Send(ItemInventoryPacket.UpdateItem(item));
        }
    }
    
    public bool MailItem(Item item) {
        lock (session.Item) {
            using GameStorage.Request db = session.GameStorage.Context();
            var mail = new Mail {
                Type = MailType.System,
                ReceiverId = session.CharacterId,
                Content = "50000000", // id from string/en/systemmailcontentna.xml
            };

            mail = db.CreateMail(mail);
            if (mail == null) {
                return false;
            }

            if (item.Uid == 0) {
                item.Slot = -1;
                Item? newAdd = db.CreateItem(mail.Id, item);
                if (newAdd == null) {
                    return false;
                }
                item = newAdd;
            }

            mail.Items.Add(item);

            try {
                session.World.MailNotification(new MailNotificationRequest {
                    CharacterId = session.CharacterId,
                    MailId = mail.Id,
                });
            } catch { /* ignored */ }
        }
        return true;
    }

    public void Save(GameStorage.Request db) {
        Equips.Save(db);
        Inventory.Save(db);
        Furnishing.Save(db);
    }
}
