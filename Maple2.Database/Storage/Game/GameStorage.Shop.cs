﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Game.Shop;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public Shop? GetShop(int shopId) {
            return Context.Shop.Find(shopId);
        }

        public IList<ShopItem> GetShopItems(int shopId) {
            return Context.ShopItem.Where(model => model.ShopId == shopId)
                .Select<Model.Shop.ShopItem, ShopItem>(item => item)
                .ToList();
        }

        public CharacterShopData? CreateCharacterShopData(long ownerId, CharacterShopData shop) {
            Model.Shop.CharacterShopData model = shop;
            model.OwnerId = ownerId;
            Context.CharacterShopData.Add(model);

            return SaveChanges() ? model : null;
        }

        public IDictionary<int, CharacterShopData> GetCharacterShopData(long ownerId) {
            return Context.CharacterShopData.Where(data => data.OwnerId == ownerId)
                .Select<Model.Shop.CharacterShopData, CharacterShopData>(data => data)
                .ToDictionary(data => data.ShopId, data => data);
        }

        public CharacterShopData? GetCharacterShopData(long ownerId, int shopId) {
            return Context.CharacterShopData.Find(shopId, ownerId);
        }

        public bool SaveCharacterShopData(long ownerId, ICollection<CharacterShopData> shopDatas) {
            foreach (CharacterShopData data in shopDatas) {
                Model.Shop.CharacterShopData model = data;
                model.OwnerId = ownerId;

                Context.CharacterShopData.Update(model);
            }

            return Context.TrySaveChanges();
        }

        public bool DeleteCharacterShopData(long ownerId, int shopId) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Model.Shop.CharacterShopData? data = Context.CharacterShopData.Find(shopId, ownerId);
            if (data == null) {
                return false;
            }

            Context.CharacterShopData.Remove(data);
            return SaveChanges();
        }


        public CharacterShopItemData? CreateCharacterShopItemData(long ownerId, CharacterShopItemData item) {
            Model.Shop.CharacterShopItemData model = item;
            model.OwnerId = ownerId;
            Context.CharacterShopItemData.Add(model);

            return SaveChanges() ? model : null;
        }

        public CharacterShopItemData? GetCharacterShopItemData(long ownerId, int shopItemId) {
            return Context.CharacterShopItemData.Find(shopItemId, ownerId);
        }

        public ICollection<CharacterShopItemData> GetCharacterShopItemData(long ownerId) {
            return Context.CharacterShopItemData.Where(data => data.OwnerId == ownerId)
                .AsEnumerable()
                .Select(ToShopItemData)
                .ToList()!;
        }

        public bool SaveCharacterShopItemData(long ownerId, ICollection<CharacterShopItemData> itemDatas) {
            foreach (CharacterShopItemData data in itemDatas) {
                Model.Shop.CharacterShopItemData model = data;
                model.OwnerId = ownerId;

                Context.CharacterShopItemData.Update(model);
            }

            return Context.TrySaveChanges();
        }

        public bool DeleteCharacterShopItemData(long ownerId, int shopItemId) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Model.Shop.CharacterShopItemData? data = Context.CharacterShopItemData.Find(shopItemId, ownerId);
            if (data == null) {
                return false;
            }

            Context.CharacterShopItemData.Remove(data);
            return SaveChanges();
        }
        
        private CharacterShopItemData? ToShopItemData(Model.Shop.CharacterShopItemData? model) {
            if (model == null) {
                return null;
            }
            if (!game.itemMetadata.TryGet(model.Item.ItemId, out ItemMetadata? metadata)) {
                return null;
            }
            Item item = model.Item.Convert(metadata);
            CharacterShopItemData data = model;
            data.Item = item;
            return data;
        }
    }
}
