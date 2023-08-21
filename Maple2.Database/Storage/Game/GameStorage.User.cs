﻿using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Database.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Account = Maple2.Model.Game.Account;
using Achievement = Maple2.Database.Model.Achievement;
using Character = Maple2.Model.Game.Character;
using SkillMacro = Maple2.Model.Game.SkillMacro;
using SkillBook = Maple2.Model.Game.SkillBook;
using SkillTab = Maple2.Model.Game.SkillTab;
using Wardrobe = Maple2.Model.Game.Wardrobe;
using GameEventUserValue = Maple2.Model.Game.GameEventUserValue;
using Home = Maple2.Model.Game.Home;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request : IPlayerInfoProvider {
        public Account? GetAccount(long accountId) {
            return Context.Account.Find(accountId);
        }

        public Account? GetAccount(string username) {
            return Context.Account
                .FirstOrDefault(account => account.Username == username);
        }

        public bool UpdateAccount(Account account, bool commit = false) {
            Context.Account.Update(account);
            if (commit) {
                return Context.TrySaveChanges();
            }

            return true;
        }

        public (Account?, IList<Character>?) ListCharacters(long accountId) {
            Model.Account? model = Context.Account
                .Include(account => account.Characters)
                .FirstOrDefault(account => account.Id == accountId);
            if (model == null) {
                return (null, null);
            }

            IList<Character>? characters = model.Characters?.Select<Model.Character, Character>(c => c).ToList();
            if (characters != null) {
                foreach (Character character in characters) {
                    character.AchievementInfo = GetAchievementInfo(accountId, character.Id);
                }    
            }

            return (model, characters);
        }

        //  If accountId is specified, only characters for the account will be returned.
        public Character? GetCharacter(long characterId, long accountId = -1) {
            if (accountId < 0) {
                Character? characterFind = Context.Character.Find(characterId);
                if (characterFind != null) {
                    characterFind.AchievementInfo = GetAchievementInfo(accountId, characterId);
                }
            }

            // Limit character fetching to those owned by account.
           Character? character = Context.Character.FirstOrDefault(character =>
                character.Id == characterId && character.AccountId == accountId);
           if (character != null) {
               character.AchievementInfo = GetAchievementInfo(accountId, characterId);
           }
           return character;
        }

        public long GetCharacterId(string name) {
            return Context.Character.Where(character => character.Name == name)
                .Select(character => character.Id)
                .FirstOrDefault();
        }

        public PlayerInfo? GetPlayerInfo(long characterId) {
            Character? character = Context.Character.FirstOrDefault(c => c.Id == characterId);
            if(character == null) return null;

            Account? account = Context.Account.FirstOrDefault(a => a.Id == character.AccountId);
            if(account == null) return null;

            IEnumerable<Achievement> accountTrophies = Context.Achievement.Where(a => a.OwnerId == character.AccountId).AsEnumerable();
            IEnumerable<Achievement> characterTrophies = Context.Achievement.Where(a => a.OwnerId == character.Id).AsEnumerable();

            UgcMap? indoor = Context.UgcMap.FirstOrDefault(u => u.OwnerId == character.AccountId && u.Indoor);
            UgcMap? outdoor = Context.UgcMap.FirstOrDefault(u => u.OwnerId == character.AccountId && !u.Indoor);

            return BuildPlayerInfo(character, indoor, outdoor, accountTrophies, characterTrophies);
        }

        public Home? GetHome(long ownerId) {
            Model.Home? model = Context.Home.Find(ownerId);
            if (model == null) {
                return null;
            }

            Home home = model;
            UgcMap[] ugcMaps = Context.UgcMap
                .Where(map => map.OwnerId == ownerId)
                .ToArray();
            PlotInfo? indoor = ToPlotInfo(ugcMaps.FirstOrDefault(map => map.Indoor));
            if (indoor == null) {
                Logger.LogError("Home does not have a indoor entry: {OwnerId}", ownerId);
                return null;
            }

            home.Indoor = indoor;
            home.Outdoor = ToPlotInfo(ugcMaps.FirstOrDefault(map => !map.Indoor));
            return home;
        }

        // We pass in objectId only for Player initialization.
        public Player? LoadPlayer(long accountId, long characterId, int objectId, short channel) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Model.Account? account = Context.Account.Find(accountId);
            if (account == null) {
                return null;
            }

            Model.Character? character = Context.Character.FirstOrDefault(character =>
                character.Id == characterId && character.AccountId == accountId);
            if (character == null) {
                return null;
            }

            account.Online = true;
            character.Channel = channel;

            Context.Account.Update(account);
            Context.Character.Update(character);
            Context.SaveChanges();

            Tuple<long, string> guild = Context.GuildMember
                .Where(member => member.CharacterId == characterId)
                .Join(Context.Guild, member => member.GuildId, guild => guild.Id,
                    (member, guild) => new Tuple<long, string>(guild.Id, guild.Name))
                .FirstOrDefault() ?? new Tuple<long, string>(0, string.Empty);

            Home? home = GetHome(accountId);
            if (home == null) {
                return null;
            }

            var player = new Player(account, character, objectId) {
                Currency = new Currency {
                    Meret = account.Currency.Meret,
                    GameMeret = account.Currency.GameMeret,
                    Meso = character.Currency.Meso,
                    EventMeret = character.Currency.EventMeret,
                    ValorToken = character.Currency.ValorToken,
                    Treva = character.Currency.Treva,
                    Rue = character.Currency.Rue,
                    HaviFruit = character.Currency.HaviFruit,
                    ReverseCoin = character.Currency.ReverseCoin,
                    MentorToken = character.Currency.MentorToken,
                    MenteeToken = character.Currency.MenteeToken,
                    StarPoint = character.Currency.StarPoint,
                    MesoToken = account.Currency.MesoToken,
                },
                Unlock = Context.CharacterUnlock.Find(characterId),
                Home = home,
            };

            player.Character.GuildId = guild.Item1;
            player.Character.GuildName = guild.Item2;

            player.Character.AchievementInfo = GetAchievementInfo(accountId, characterId);

            return player;
        }

        public bool SavePlayer(Player player) {
            Console.WriteLine($"> Begin Save... {Context.ContextId}");

            Model.Account account = player.Account;
            account.Currency = new AccountCurrency {
                Meret = player.Currency.Meret,
                GameMeret = player.Currency.GameMeret,
                MesoToken = player.Currency.MesoToken,
            };
            
            Model.Character character = player.Character;
            character.Currency = new CharacterCurrency {
                Meso = player.Currency.Meso,
                EventMeret = player.Currency.EventMeret,
                ValorToken = player.Currency.ValorToken,
                Treva = player.Currency.Treva,
                Rue = player.Currency.Rue,
                HaviFruit = player.Currency.HaviFruit,
                ReverseCoin = player.Currency.ReverseCoin,
                MentorToken = player.Currency.MentorToken,
                MenteeToken = player.Currency.MenteeToken,
                StarPoint = player.Currency.StarPoint,
            };

            Context.Update(account);
            Context.Update(character);

            CharacterUnlock unlock = player.Unlock;
            unlock.CharacterId = character.Id;
            Context.Update(unlock);

            Context.ChangeTracker.Entries().DisplayStates();
            return Context.TrySaveChanges();
        }

        public (IList<KeyBind>? KeyBinds, IList<QuickSlot[]>? HotBars, List<SkillMacro>?, List<Wardrobe>?, List<int>? FavoriteStickers, IDictionary<LapenshardSlot, int>? Lapenshards, IDictionary<BasicAttribute, int>?, SkillBook?) LoadCharacterConfig(long characterId) {
            CharacterConfig? config = Context.CharacterConfig.Find(characterId);
            if (config == null) {
                return (null, null, null, null, null, null, null, null);
            }

            SkillBook? skillBook = config.SkillBook == null ? null : new SkillBook {
                MaxSkillTabs = config.SkillBook.MaxSkillTabs,
                ActiveSkillTabId = config.SkillBook.ActiveSkillTabId,
                SkillTabs = Context.SkillTab.Where(tab => tab.CharacterId == characterId)
                    .Select<Model.SkillTab, SkillTab>(tab => tab)
                    .ToList(),
            };

            Dictionary<GameEventUserValueType, GameEventUserValue> eventValues = Context.GameEventUserValue.Where(value => value.CharacterId == characterId)
                .Select<Model.Event.GameEventUserValue, GameEventUserValue>(value => value)
                .ToDictionary(value => value.Type, value => value);

            return (
                config.KeyBinds,
                config.HotBars,
                config.SkillMacros?.Select<Model.SkillMacro, SkillMacro>(macro => macro).ToList(),
                config.Wardrobes?.Select<Model.Wardrobe, Wardrobe>(wardrobe => wardrobe).ToList(),
                config.FavoriteStickers?.Select(stickers => stickers).ToList(),
                config.Lapenshards,
                config.StatAllocation,
                skillBook
            );
        }

        public bool SaveCharacterConfig(
            long characterId,
            IList<KeyBind> keyBinds,
            IList<QuickSlot[]> hotBars,
            IEnumerable<SkillMacro> skillMacros,
            IEnumerable<Wardrobe> wardrobes,
            IList<int> favoriteStickers,
            IDictionary<LapenshardSlot, int> lapenshards,
            StatAttributes.PointAllocation allocation,
            SkillBook skillBook) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            CharacterConfig? config = Context.CharacterConfig.Find(characterId);
            if (config == null) {
                return false;
            }

            config.KeyBinds = keyBinds;
            config.HotBars = hotBars;
            config.SkillMacros = skillMacros.Select<SkillMacro, Model.SkillMacro>(macro => macro).ToList();
            config.Wardrobes = wardrobes.Select<Wardrobe, Model.Wardrobe>(wardrobe => wardrobe).ToList();
            config.FavoriteStickers = favoriteStickers;
            config.Lapenshards = lapenshards;
            config.StatAllocation = allocation.Attributes.ToDictionary(
                attribute => attribute,
                attribute => allocation[attribute]);
            config.SkillBook = new Model.SkillBook {
                MaxSkillTabs = skillBook.MaxSkillTabs,
                ActiveSkillTabId = skillBook.ActiveSkillTabId,
            };
            Context.CharacterConfig.Update(config);

            foreach (SkillTab skillTab in skillBook.SkillTabs) {
                Model.SkillTab model = skillTab;
                model.CharacterId = characterId;
                Context.SkillTab.Update(model);
            }

            return Context.TrySaveChanges();
        }

        #region Create
        public Account CreateAccount(Account account) {
            Model.Account model = account;
            model.Id = 0;
#if DEBUG
            model.Currency = new AccountCurrency {Meret = 99999};
#endif
            Context.Account.Add(model);
            Context.SaveChanges(); // Exception if failed.

            Context.Home.Add(new Home {AccountId = model.Id});
            Context.UgcMap.Add(new UgcMap {
                OwnerId = model.Id,
                MapId = Constant.DefaultHomeMapId,
                Indoor = true,
                Number = Constant.DefaultHomeNumber,
            });
            Context.SaveChanges(); // Exception if failed.

            return model;
        }

        public Character? CreateCharacter(Character character) {
            Model.Character model = character;
            model.Id = 0;
#if DEBUG
            model.Currency = new CharacterCurrency {Meso = 999999999};
#endif
            Context.Character.Add(model);
            return Context.TrySaveChanges() ? model : null;
        }

        public bool InitNewCharacter(long characterId, Unlock unlock) {
            CharacterUnlock model = unlock;
            model.CharacterId = characterId;
            Context.CharacterUnlock.Add(model);

            SkillTab? defaultTab = CreateSkillTab(characterId, new SkillTab("Build 1") {Id = characterId});
            if (defaultTab == null) {
                return false;
            }

            var config = new CharacterConfig {
                CharacterId = characterId,
                SkillBook = new Model.SkillBook {
                    MaxSkillTabs = 1,
                    ActiveSkillTabId = defaultTab.Id,
                },
            };
            Context.CharacterConfig.Add(config);

            return Context.TrySaveChanges();
        }

        public SkillTab? CreateSkillTab(long characterId, SkillTab skillTab) {
            Model.SkillTab model = skillTab;
            model.CharacterId = characterId;
            Context.SkillTab.Add(model);
            return Context.TrySaveChanges() ? model : null;
        }
        #endregion

        #region Delete
        public bool UpdateDelete(long accountId, long characterId, long time) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Model.Character? model = Context.Character.FirstOrDefault(character =>
                character.Id == characterId && character.AccountId == accountId);
            if (model == null) {
                return false;
            }

            model.DeleteTime = time.FromEpochSeconds();
            Context.Update(model);
            return Context.TrySaveChanges();
        }

        public bool DeleteCharacter(long accountId, long characterId) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Model.Character? character = Context.Character.FirstOrDefault(character =>
                character.Id == characterId && character.AccountId == accountId);
            if (character == null) {
                return false;
            }

            Context.Remove(character);
            return Context.TrySaveChanges();
        }
        #endregion
    }

}
