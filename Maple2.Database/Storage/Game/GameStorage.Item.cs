﻿using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Database.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game.Event;
using Maple2.Model.Metadata;
using Item = Maple2.Model.Game.Item;
using PetConfig = Maple2.Model.Game.PetConfig;
using UgcItemLook = Maple2.Model.Game.UgcItemLook;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public Item? CreateItem(long ownerId, Item item) {
            Model.Item model = item;
            model.OwnerId = ownerId;
            model.Id = 0;
            Context.Item.Add(model);

            /*GameEvent gameEvent = new GameEvent() {
                BeginTime = DateTime.Now.ToEpochSeconds(),
                EndTime = DateTime.Now.AddDays(30).ToEpochSeconds(),
                Name = nameof(AttendGift),
                EventInfo = new AttendGift() {
                    AttendanceName = "Emulator Attendance",
                    BeginTime = DateTime.Now.ToEpochSeconds(),
                    EndTime = DateTime.Now.AddDays(30).ToEpochSeconds(),
                    Days = new List<AttendGift.AttendanceDayItem>() {
                        new AttendGift.AttendanceDayItem {
                            Day = 1,
                            ItemRarity = 4,
                            ItemId = 20600010,
                            ItemAmount = 1,
                        },
                        new AttendGift.AttendanceDayItem {
                            Day = 2,
                            ItemRarity = 1,
                            ItemId = 30001445,
                            ItemAmount = 20,
                        },
                        new AttendGift.AttendanceDayItem {
                            Day = 3,
                            ItemRarity = 2,
                            ItemId = 20800015,
                            ItemAmount = 20,
                        },
                        new AttendGift.AttendanceDayItem {
                            Day = 4,
                            ItemRarity = 4,
                            ItemId = 22001001,
                            ItemAmount = 1,
                        },
                        new AttendGift.AttendanceDayItem {
                            Day = 5,
                            ItemRarity = 4,
                            ItemId = 20500010,
                            ItemAmount = 1,
                        },
                        new AttendGift.AttendanceDayItem {
                            Day = 6,
                            ItemRarity = 1,
                            ItemId = 20303152,
                            ItemAmount = 1,
                        },
                        new AttendGift.AttendanceDayItem {
                            Day = 7,
                            ItemRarity = 1,
                            ItemId = 20302945,
                            ItemAmount = 1,
                        },
                        new AttendGift.AttendanceDayItem {
                            Day = 8,
                            ItemRarity = 1,
                            ItemId = 30001445,
                            ItemAmount = 100,
                        },
                        new AttendGift.AttendanceDayItem {
                            Day = 9,
                            ItemRarity = 4,
                            ItemId = 70600011,
                            ItemAmount = 1,
                        },
                        new AttendGift.AttendanceDayItem {
                            Day = 10,
                            ItemRarity = 4,
                            ItemId = 34000098,
                            ItemAmount = 1,
                        },
                        new AttendGift.AttendanceDayItem {
                            Day = 11,
                            ItemRarity = 4,
                            ItemId = 40400063,
                            ItemAmount = 1,
                        },
                        new AttendGift.AttendanceDayItem {
                            Day = 12,
                            ItemRarity = 1,
                            ItemId = 59400040,
                            ItemAmount = 1,
                        },
                        new AttendGift.AttendanceDayItem {
                            Day = 13,
                            ItemRarity = 5,
                            ItemId = 20000702,
                            ItemAmount = 200,
                        },
                        new AttendGift.AttendanceDayItem {
                            Day = 14,
                            ItemRarity = 4,
                            ItemId = 20200104,
                            ItemAmount = 200,
                        },
                    },
                    DisableClaimButton = true,
                    Name = nameof(AttendGift),
                    SkipDayCost = 500,
                    SkipDayCurrencyType = AttendGiftCurrencyType.Meret,
                    SkipDaysAllowed = 5,
                    TimeRequired = 1800,
                    Url = "https://google.com",
                },
            };
            Model.Event.GameEvent gameModel = gameEvent;
            Context.GameEvent.Add(gameModel);
            Context.TrySaveChanges();*/

            return Context.TrySaveChanges() ? ToItem(model) : null;
        }

        public Item? SplitItem(long ownerId, Item item, int amount) {
            Model.Item model = item;
            model.Amount = amount;
            model.OwnerId = ownerId;
            model.Slot = -1;
            model.Group = ItemGroup.Default;
            model.Id = 0;
            Context.Item.Add(model);

            return Context.TrySaveChanges() ? ToItem(model) : null;
        }

        public List<Item>? CreateItems(long ownerId, params Item[] items) {
            var models = new Model.Item[items.Length];
            for (int i = 0; i < items.Length; i++) {
                models[i] = items[i];
                models[i].OwnerId = ownerId;
                models[i].Id = 0;
                Context.Item.Add(models[i]);
            }

            if (!Context.TrySaveChanges()) {
                return null;
            }

            return models.Select(ToItem).Where(item => item != null).ToList()!;
        }

        public Item? GetItem(long itemUid) {
            Model.Item? model = Context.Item.Find(itemUid);
            if (model == null) {
                return null;
            }

            return game.itemMetadata.TryGet(model.ItemId, out ItemMetadata? metadata) ? model.Convert(metadata) : null;
        }

        public UgcItemLook? GetTemplate(long itemUid) {
            ItemSubType? model = Context.Item.Select(item => new {item.Id, item.SubType})
                .FirstOrDefault(result => result.Id == itemUid)?.SubType;
            if (model is not ItemUgc ugcModel) {
                return null;
            }

            return ugcModel.Template;
        }
        public IDictionary<ItemGroup, List<Item>> GetItemGroups(long ownerId, params ItemGroup[] groups) {
            return Context.Item.Where(item => item.OwnerId == ownerId && groups.Contains(item.Group))
                .AsEnumerable()
                .GroupBy(item => item.Group)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(ToItem).Where(item => item != null).ToList()
                )!;
        }

        public Dictionary<InventoryType, List<Item>> GetInventory(long characterId) {
            return Context.Item.Where(item => item.OwnerId == characterId && item.Group == ItemGroup.Default)
                .AsEnumerable()
                .Select(ToItem)
                .Where(item => item != null)
                .GroupBy(item => item!.Inventory)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList()
                )!;
        }

        public (long Mesos, short Expand) GetStorageInfo(long accountId) {
            ItemStorage? info = Context.ItemStorage.Find(accountId);
            if (info == null) {
                return (0, 0);
            }

            return (info.Meso, info.Expand);
        }

        public PetConfig GetPetConfig(long itemUid) {
            return Context.PetConfig.Find(itemUid) ?? new PetConfig();
        }

        public List<Item> GetStorage(long accountId) {
            return Context.Item.Where(item => item.OwnerId == accountId && item.Group == ItemGroup.Default)
                .AsEnumerable()
                .Select(ToItem)
                .Where(item => item != null)
                .ToList()!;
        }

        public List<Item> GetAllItems(long ownerId) {
            return Context.Item.Where(item => item.OwnerId == ownerId)
                .AsEnumerable()
                .Select(ToItem)
                .Where(item => item != null)
                .ToList()!;
        }

        public bool SaveItems(long ownerId, params Item[] items) {
            var models = new Model.Item[items.Length];
            for (int i = 0; i < items.Length; i++) {
                if (items[i].Uid == 0) {
                    continue;
                }

                models[i] = items[i];
                models[i].OwnerId = ownerId;
                Context.Item.Update(models[i]);
            }

            return Context.TrySaveChanges();
        }

        public bool SaveStorageInfo(long accountId, long mesos, short expand) {
            ItemStorage? info = Context.ItemStorage.Find(accountId);
            if (info == null) {
                Context.Add(new ItemStorage {
                    AccountId = accountId,
                    Meso = mesos,
                    Expand = expand,
                });
            } else {
                info.Meso = mesos;
                info.Expand = expand;
                Context.ItemStorage.Update(info);
            }

            return Context.TrySaveChanges();
        }

        public bool SavePetConfig(long itemUid, PetConfig config) {
            Model.PetConfig? model = Context.PetConfig.Find(itemUid);
            if (model == null) {
                model = config;
                model.ItemUid = itemUid;

                Context.Add(model);
            } else {
                model = config;
                model.ItemUid = itemUid;

                Context.Update(model);
            }

            return Context.TrySaveChanges();
        }

        // Converts model to item if possible, otherwise returns null.
        private Item? ToItem(Model.Item? model) {
            if (model == null) {
                return null;
            }

            return game.itemMetadata.TryGet(model.ItemId, out ItemMetadata? metadata) ? model.Convert(metadata) : null;
        }
    }
}
