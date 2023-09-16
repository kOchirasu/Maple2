using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Maple2.Database.Extensions;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Game.Shop;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;
using Maple2.Tools.Extensions;

using Serilog;

namespace Maple2.Server.Game.Manager;

public sealed class ShopManager {
    #region EntryId
    private int idCounter;

    /// <summary>
    /// Generates an EntryId unique to this specific actor instance.
    /// </summary>
    /// <returns>Returns a local EntryId</returns>
    private int NextEntryId() => Interlocked.Increment(ref idCounter);
    #endregion
    private readonly GameSession session;
    private IDictionary<int, CharacterShopData> accountShopData;
    private IDictionary<int, CharacterShopData> characterShopData;
    private IDictionary<int, Shop> instancedShops;
    private IDictionary<int, IDictionary<int, CharacterShopItemData>> characterShopItemData;
    private IDictionary<int, IDictionary<int, CharacterShopItemData>> accountShopItemData;
    private IDictionary<int, BuyBackItem> buyBackItems;
    private Shop? activeShop;
    private int shopSourceId;

    private readonly ILogger logger = Log.Logger.ForContext<ShopManager>();

    public ShopManager(GameSession session) {
        this.session = session;
        instancedShops = new Dictionary<int, Shop>();
        buyBackItems = new Dictionary<int, BuyBackItem>();
        using GameStorage.Request db = session.GameStorage.Context();

        accountShopData = new Dictionary<int, CharacterShopData>();
        IDictionary<int, CharacterShopData> accountShopDictionary = db.GetCharacterShopData(session.AccountId);
        foreach (CharacterShopData data in accountShopDictionary.Values) {
            accountShopData[data.ShopId] = data;
        }

        characterShopData = new Dictionary<int, CharacterShopData>();
        IDictionary<int, CharacterShopData> characterShopDictionary = db.GetCharacterShopData(session.CharacterId);
        foreach (CharacterShopData data in characterShopDictionary.Values) {
            characterShopData[data.ShopId] = data;
        }

        accountShopItemData = new Dictionary<int, IDictionary<int, CharacterShopItemData>>();
        ICollection<CharacterShopItemData> accountShopItemList = db.GetCharacterShopItemData(session.AccountId);
        foreach (CharacterShopItemData itemData in accountShopItemList) {
            if (accountShopItemData.TryGetValue(itemData.ShopId, out IDictionary<int, CharacterShopItemData>? itemDictionary)) {
                if (itemDictionary.ContainsKey(itemData.ShopItemId)) {
                    continue;
                }
                itemDictionary[itemData.ShopItemId] = itemData;
                continue;
            }
            accountShopItemData[itemData.ShopId] = new Dictionary<int, CharacterShopItemData> {
                [itemData.ShopItemId] = itemData,
            };
        }

        characterShopItemData = new Dictionary<int, IDictionary<int, CharacterShopItemData>>();
        ICollection<CharacterShopItemData> characterShopItemList = db.GetCharacterShopItemData(session.CharacterId);
        foreach (CharacterShopItemData itemData in characterShopItemList) {
            if (characterShopItemData.TryGetValue(itemData.ShopId, out IDictionary<int, CharacterShopItemData>? itemDictionary)) {
                if (itemDictionary.ContainsKey(itemData.ShopItemId)) {
                    continue;
                }
                itemDictionary[itemData.ShopItemId] = itemData;
                continue;
            }
            characterShopItemData[itemData.ShopId] = new Dictionary<int, CharacterShopItemData> {
                [itemData.ShopItemId] = itemData,
            };
        }
    }

    private bool TryGetShopData(int shopId, [NotNullWhen(true)] out CharacterShopData? data) {
        return accountShopData.TryGetValue(shopId, out data) || characterShopData.TryGetValue(shopId, out data);
    }

    private bool TryGetShopItemData(int shopId, [NotNullWhen(true)] out IDictionary<int, CharacterShopItemData>? data) {
        return accountShopItemData.TryGetValue(shopId, out data) || characterShopItemData.TryGetValue(shopId, out data);
    }

    public void Load(int shopId, int sourceId = 0) {
        Shop? shop = session.FindShop(shopId);
        if (shop == null) {
            logger.Warning("Shop {Id} has not been implemented", shopId);
            return;
        }

        if (shop.RestockData != null) {
            shop = GetInstancedShop(shop);
            if (shop == null) {
                logger.Error("Failed to create instanced shop for {Id}", shopId);
                return;
            }
        }

        activeShop = shop;
        shopSourceId = sourceId;

        session.Send(ShopPacket.Open(activeShop, shopSourceId));
        session.Send(ShopPacket.LoadItems(shop.Items.Values));
        if (!shop.DisableBuyback) {
            session.Send(ShopPacket.BuyBackItemCount((short) buyBackItems.Count));
            if (buyBackItems.Count > 0) {
                session.Send(ShopPacket.LoadBuyBackItem((BuyBackItem[]) buyBackItems.Values));
            }
        }
    }

    private Shop? GetInstancedShop(Shop shop) {
        if (instancedShops.TryGetValue(shop.Id, out Shop? instancedShop)) {
            if (instancedShop.RestockTime < DateTime.Now.ToEpochSeconds()) {
                return CreateInstancedShop(shop);
            }
            // also need to apply shop data
            // ApplyItemData(instancedShop);
            return instancedShop;
        }


        if (!TryGetShopData(shop.Id, out CharacterShopData? data) || data.RestockTime < DateTime.Now.ToEpochSeconds()) {
            return CreateInstancedShop(shop);
        }

        // assemble shop
        instancedShop = shop.Clone()!;
        instancedShop.RestockTime = data.RestockTime;
        instancedShop.Items = GetShopItems(shop); // TODO: This is bad because we're making items to just replace it right after. Maybe an alt method.
        ApplyItemData(instancedShop);
        instancedShops[instancedShop.Id] = instancedShop;
        return instancedShop;
    }

    private Shop CreateInstancedShop(Shop shop) {
        Shop instancedShop = shop.Clone()!;

        // First delete any existing shop and shop item data.
        DeleteShopData(shop);

        // Create new shop data
        CharacterShopData data = CreateShopData(instancedShop);
        instancedShop.Items = GetShopItems(shop);
        CreateShopItemData(instancedShop);
        instancedShops[instancedShop.Id] = instancedShop;
        return instancedShop;
    }

    private void DeleteShopData(Shop shop) {
        if (!TryGetShopData(shop.Id, out CharacterShopData? _)) {
            return;
        }
        long ownerId = shop.RestockData!.PersistantInventory ? session.AccountId : session.CharacterId;
        using GameStorage.Request db = session.GameStorage.Context();
        db.DeleteCharacterShopData(ownerId, shop.Id);

        if (!TryGetShopItemData(shop.Id, out IDictionary<int, CharacterShopItemData>? shopItemDatas)) {
            return;
        }

        foreach ((int shopItemId, CharacterShopItemData? itemData) in shopItemDatas) {
            db.DeleteCharacterShopItemData(ownerId, shopItemId);
        }
    }

    private void ApplyItemData(Shop shop) {
        if (!TryGetShopItemData(shop.Id, out IDictionary<int, CharacterShopItemData>? itemData)) {
            itemData = new Dictionary<int, CharacterShopItemData>(); // this is temporary
            // doesnt exist..create here? should this even happen?
        }
        foreach ((int shopItemId, ShopItem item) in shop.Items) {
            if (!itemData.TryGetValue(shopItemId, out CharacterShopItemData? data)) {
                continue;
            }

            item.Item = data.Item;
            item.StockPurchased = data.StockPurchased;
        }
    }

    private IDictionary<int, ShopItem> GetShopItems(Shop shop) {
        IDictionary<int, ShopItem> items = new Dictionary<int, ShopItem>();

        using GameStorage.Request db = session.GameStorage.Context();

        if (shop.RestockData?.Interval == ShopRestockInterval.Minute) {
            //TODO: Get from cache, not DB
            IEnumerable<ShopItem> itemList = db.GetShopItems(shop.Id).OrderBy(_ => Random.Shared.Next()).Take(12);
            foreach(ShopItem shopItem in itemList) {
                Item? item = session.Item.CreateItem(shopItem.ItemId, shopItem.Rarity, shopItem.Quantity);
                if (item == null) {
                    continue;
                }
                shopItem.Item = item;
                items.Add(shopItem.Id, shopItem.Clone());
            }
        } else {
            foreach ((int id, ShopItem shopItem) in shop.Items) {
                Item? item = session.Item.CreateItem(shopItem.ItemId, shopItem.Rarity, shopItem.Quantity);
                if (item == null) {
                    continue;
                }
                items.Add(id, shopItem.Clone());
            }
        }

        return items;
    }

    private CharacterShopData CreateShopData(Shop shop) {
        var data = new CharacterShopData {
            Interval = shop.RestockData!.Interval,
            RestockTime = GetRestockTime(shop.RestockData!.Interval),
            ShopId = shop.Id,
        };

        shop.RestockTime = data.RestockTime;

        // We do not save shops with minute intervals to database
        if (shop.RestockData.Interval != ShopRestockInterval.Minute) {
            using GameStorage.Request db = session.GameStorage.Context();
            long ownerId = shop.RestockData.PersistantInventory ? session.AccountId : session.CharacterId;
            data = db.CreateCharacterShopData(ownerId, data);
            if (data == null) {
                throw new InvalidOperationException($"Failed to create shop data {shop.Id} for character {session.CharacterId}");
            }
        }

        if (shop.RestockData.PersistantInventory) {
            accountShopData[shop.Id] = data;
        } else {
            characterShopData[shop.Id] = data;
        }
        return data;
    }

    private void CreateShopItemData(Shop shop) {
        Dictionary<int, CharacterShopItemData> itemData = new Dictionary<int, CharacterShopItemData>();
        using GameStorage.Request db = session.GameStorage.Context();
        long ownerId = shop.RestockData!.PersistantInventory ? session.AccountId : session.CharacterId;
        foreach ((int id, ShopItem item) in shop.Items) {
            if (item.StockCount > 0) {
                var data = new CharacterShopItemData {
                    ShopId = shop.Id,
                    ShopItemId = id,
                    Item = item.Item,
                };
                data = db.CreateCharacterShopItemData(ownerId, data);
                if (data == null) {
                    //BROKEN
                    continue;
                }
                itemData[id] = data;
            }
        }

        if (shop.RestockData!.PersistantInventory) {
            accountShopItemData[shop.Id] = itemData;
        } else {
            characterShopItemData[shop.Id] = itemData;
        }
    }

    public void InstantRestock() {
        if (activeShop == null) {
            return;
        }

        if (activeShop.RestockData == null || activeShop.RestockData.DisableInstantRestock) {
            return;
        }

        // TODO: Do excess cost
        if (!Pay(new ShopCost {
                Amount = activeShop.RestockData.Cost,
                Type = activeShop.RestockData.CurrencyType,
                ItemId = 0,
                SaleAmount = 0,
            }, activeShop.RestockData.Cost)) {
            return;
        }


        activeShop.RestockData.RestockCount = 0;
        activeShop.RestockTime = GetInstantRestockTime(activeShop.RestockData.Interval);
        UpdateShopData(activeShop);
        session.Send(ShopPacket.InstantRestock());
        session.Send(ShopPacket.Open(activeShop, shopSourceId));
        session.Send(ShopPacket.LoadItems(activeShop.Items.Values));
    }

    public void Refresh() {
        if (activeShop?.RestockData == null) {
            return;
        }

        activeShop.RestockData.RestockCount = 0;
        activeShop.RestockTime = GetRestockTime(activeShop.RestockData.Interval);
        UpdateShopData(activeShop);
        activeShop.Items = GetShopItems(activeShop);
        session.Send(ShopPacket.Open(activeShop, shopSourceId));
        session.Send(ShopPacket.LoadItems(activeShop.Items.Values));
    }

    private void UpdateShopData(Shop shop) {
        if (!TryGetShopData(shop.Id, out CharacterShopData? data)) {
            return;
        }

        data.RestockCount = 0;
        data.RestockTime = shop.RestockTime;
    }

    private void UpdateStockCount(int shopId, ShopItem shopItem, int quantity) {
        if (!TryGetShopItemData(shopId, out IDictionary<int, CharacterShopItemData>? data) ||
            !data.TryGetValue(shopItem.Id, out CharacterShopItemData? itemData)) {
            return;
        }
        itemData.StockPurchased += quantity;
        shopItem.StockPurchased += quantity;
    }

    private long GetRestockTime(ShopRestockInterval interval) {
        // Follows a time schedule
        DateTime now = DateTime.Now;
        switch (interval) {
            case ShopRestockInterval.Minute:
                return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1).ToEpochSeconds();
            case ShopRestockInterval.Day:
                return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(1).ToEpochSeconds();
            case ShopRestockInterval.Week:
                return DateTime.Now.NextDayOfWeek(Constant.ResetDay).Date.ToEpochSeconds();
            case ShopRestockInterval.Month:
                return new DateTime(now.Year, now.Month, 1, 0, 0, 0).AddMonths(1).ToEpochSeconds();
            default:
                logger.Error("Unknown restock interval {Interval}", interval);
                return long.MaxValue;
        }
    }

    private long GetInstantRestockTime(ShopRestockInterval interval) {
        return interval switch {
            ShopRestockInterval.Minute => DateTime.Now.AddMinutes(1).ToEpochSeconds(),
            ShopRestockInterval.Day => DateTime.Now.AddDays(1).ToEpochSeconds(),
            ShopRestockInterval.Week => DateTime.Now.AddDays(7).ToEpochSeconds(),
            ShopRestockInterval.Month => DateTime.Now.AddMonths(1).ToEpochSeconds(),
            _ => long.MaxValue,

        };
    }


    public void Buy(int shopItemId, int quantity) {
        if (activeShop == null) {
            return;
        }

        if (!activeShop.Items.TryGetValue(shopItemId, out ShopItem? shopItem)) {
            session.Send(ShopPacket.Error(ShopError.s_err_invalid_item));
            return;
        }

        if (shopItem.StockCount > 0 && shopItem.StockPurchased + quantity > shopItem.StockCount) {
            session.Send(ShopPacket.Error(ShopError.s_err_lack_shopitem));
            return;
        }

        if (shopItem.RequireAchievementId > 0 && !session.Achievement.HasAchievement(shopItem.RequireAchievementId, shopItem.RequireAchievementRank)) {
            return;
        }

        if (shopItem.RequireGuildTrophy > 0 && session.Guild.Guild != null && session.Guild.Guild.AchievementInfo.Total < shopItem.RequireGuildTrophy) {
            session.Send(ShopPacket.Error(ShopError.s_err_lack_guild_trophy));
            return;
        }

        // TODO: Guild Merchant Type/Level, Championship, Alliance

        Item item = shopItem.Item.Clone();
        item.Amount = quantity;
        if (!session.Item.Inventory.CanAdd(item)) {
            session.Send(ShopPacket.Error(ShopError.s_err_inventory));
            return;
        }

        int price = shopItem.Cost.SaleAmount > 0 ? shopItem.Cost.SaleAmount * quantity : shopItem.Cost.Amount * quantity;
        if (!Pay(shopItem.Cost, price)) {
            return;
        }

        if (activeShop.RestockData != null && shopItem.StockCount > 0) {
            UpdateStockCount(activeShop.Id, shopItem, quantity);
            session.Send(ShopPacket.Update(shopItem.Id, quantity * shopItem.StockPurchased));
        }

        session.Item.Inventory.Add(item, true);
        session.Send(ShopPacket.Buy(shopItem, quantity, price));
    }

    public void PurchaseBuyBack(int id) {
        if (activeShop == null) {
            return;
        }

        if (!buyBackItems.TryGetValue(id, out BuyBackItem? buyBackItem)) {
            session.Send(ShopPacket.Error(ShopError.s_err_invalid_item));
            return;
        }

        if (!session.Item.Inventory.CanAdd(buyBackItem.Item)) {
            session.Send(ShopPacket.Error(ShopError.s_err_inventory));
            return;
        }

        if (!Pay(new ShopCost {
                Type = ShopCurrencyType.Meso,
                Amount = (int) buyBackItem.Price,
            }, (int) buyBackItem.Price)) {
            return;
        }

        session.Item.Inventory.Add(buyBackItem.Item, true);
        buyBackItems.Remove(id);
        session.Send(ShopPacket.RemoveBuyBackItem(id));
    }

    public void Sell(long itemUid, int quantity) {
        if (activeShop == null) {
            return;
        }

        if (activeShop.DisableBuyback) {
            session.Send(ShopPacket.Error(ShopError.s_msg_cant_sell_to_only_sell_shop));
            return;
        }

        Item? item = session.Item.Inventory.Get(itemUid);
        if (item == null || !item.Metadata.Limit.ShopSell) {
            return;
        }

        long sellPrice = Core.Formulas.Shop.SellPrice(item.Metadata, item.Type, item.Rarity);
        if (session.Currency.CanAddMeso(sellPrice) != sellPrice) {
            return;
        }

        session.Currency.Meso += sellPrice;

        if (buyBackItems.Count >= Constant.MaxBuyBackItems) {
            RemoveBuyBackItem();
        }

        if (!session.Item.Inventory.Remove(item.Uid, out item, quantity)) {
            logger.Error("Failed to remove item {Uid} from inventory", itemUid);
            return;
        }

        int entryId = NextEntryId();
        buyBackItems[entryId] = new BuyBackItem {
            Id = entryId,
            Item = item,
            AddedTime = DateTime.Now.ToEpochSeconds(),
            Price = sellPrice,
        };

        session.Send(ShopPacket.LoadBuyBackItem(buyBackItems[entryId]));
    }

    private bool Pay(ShopCost cost, int price) {
        switch (cost.Type) {
            case ShopCurrencyType.Meso:
                if (session.Currency.CanAddMeso(-price) != -price) {
                    session.Send(ShopPacket.Error(ShopError.s_err_lack_meso));
                    return false;
                }
                session.Currency.Meso -= price;
                break;
            case ShopCurrencyType.Meret:
            case ShopCurrencyType.EventMeret:
                if (session.Currency.CanAddMeret(-price) != -price) {
                    session.Send(ShopPacket.Error(ShopError.s_err_lack_merat));
                    return false;
                }

                session.Currency.Meret -= price;
                break;
            case ShopCurrencyType.GameMeret:
                if (session.Currency.CanAddGameMeret(-price) != -price) {
                    session.Send(ShopPacket.Error(ShopError.s_err_lack_merat));
                    return false;
                }

                session.Currency.Meret -= price;
                break;
            case ShopCurrencyType.Item:
                var ingredient = new ItemComponent(cost.ItemId, -1, price, ItemTag.None);
                if (!session.Item.Inventory.ConsumeItemComponents(new[] {
                        ingredient
                    })) {
                    session.Send(ShopPacket.Error(ShopError.s_err_lack_payment_item, cost.ItemId));
                    return false;
                }
                break;
            default:
                CurrencyType currencyType = cost.Type switch {
                    ShopCurrencyType.ValorToken => CurrencyType.ValorToken,
                    ShopCurrencyType.Treva => CurrencyType.Treva,
                    ShopCurrencyType.Rue => CurrencyType.Rue,
                    ShopCurrencyType.HaviFruit => CurrencyType.HaviFruit,
                    ShopCurrencyType.StarPoint => CurrencyType.StarPoint,
                    ShopCurrencyType.MenteeToken => CurrencyType.MenteeToken,
                    ShopCurrencyType.MentorToken => CurrencyType.MentorToken,
                    ShopCurrencyType.MesoToken => CurrencyType.MesoToken,
                    ShopCurrencyType.ReverseCoin => CurrencyType.ReverseCoin,
                    _ => CurrencyType.None,

                };
                if (currencyType == CurrencyType.None || session.Currency[currencyType] < price) {
                    return false;
                }

                session.Currency[currencyType] -= cost.Amount;
                break;
        }
        return true;
    }

    private void RemoveBuyBackItem() {
        while (buyBackItems.Count >= Constant.MaxBuyBackItems) {
            BuyBackItem? item = buyBackItems.Values.MinBy(entry => entry.AddedTime);
            if (item == null) {
                logger.Error("Failed to remove buyback item");
                return;
            }

            buyBackItems.Remove(item.Id);
            session.Item.Inventory.Discard(item.Item);
            session.Send(ShopPacket.RemoveBuyBackItem(item.Id));
        }
    }

    public void Save(GameStorage.Request db) {
        foreach (BuyBackItem item in buyBackItems.Values) {
            session.Item.Inventory.Discard(item.Item);
        }

        db.SaveCharacterShopData(session.AccountId, accountShopData.Values.Where(value => value.Interval != ShopRestockInterval.Minute).ToList());
        db.SaveCharacterShopData(session.CharacterId, characterShopData.Values.Where(value => value.Interval != ShopRestockInterval.Minute).ToList());
        db.SaveCharacterShopItemData(session.AccountId, accountShopItemData.Values.SelectMany(value => value.Values).ToList());
        db.SaveCharacterShopItemData(session.CharacterId, characterShopItemData.Values.SelectMany(value => value.Values).ToList());
    }
}
