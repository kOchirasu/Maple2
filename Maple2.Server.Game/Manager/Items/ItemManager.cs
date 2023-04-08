﻿using System;
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

    public Item CreateItem(ItemMetadata itemMetadata, int rarity = 1, int amount = 1) {
        Item item = session.Item.CreateItem(itemMetadata, rarity, amount);
        item.Stats = itemStatsCalc.GetStats(item);
        item.Socket = itemStatsCalc.GetSockets(item);

        if (item.Appearance != null) {
            item.Appearance.Color = GetColor(item.Metadata.Customize);
        }

        return item;
    }
    
    private EquipColor GetColor(ItemMetadataCustomize metadata) {
        // Item has no color
        if (metadata.ColorPalette == 0 || 
            !session.TableMetadata.ColorPaletteTable.Entries.TryGetValue(metadata.ColorPalette, out Dictionary<int, ColorPaletteTable.Entry>? palette)) {
            return default;
        }
        
        // Item has random color
        if (metadata.DefaultColorIndex < 0) {
            // random entry from palette
            int index = Random.Shared.Next(palette.Count);
            ColorPaletteTable.Entry randomEntry = palette.Values.ElementAt(index);
            return new EquipColor(randomEntry.Primary, randomEntry.Secondary, randomEntry.Tertiary, metadata.ColorPalette, index);
        }
        
        // Item has specified color
        if (palette.TryGetValue(metadata.DefaultColorIndex, out ColorPaletteTable.Entry? entry)) {
            return new EquipColor(entry.Primary, entry.Secondary, entry.Tertiary, metadata.ColorPalette, metadata.DefaultColorIndex);
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
