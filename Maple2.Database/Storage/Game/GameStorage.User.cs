using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Database.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Config;
using Microsoft.EntityFrameworkCore;
using Account = Maple2.Model.Game.Account;
using Character = Maple2.Model.Game.Character;
using Home = Maple2.Database.Model.Home;
using Plot = Maple2.Model.Game.Plot;
using SkillMacro = Maple2.Model.Game.SkillMacro;
using SkillBook = Maple2.Model.Game.SkillBook;
using SkillTab = Maple2.Model.Game.SkillTab;
using Wardrobe = Maple2.Model.Game.Wardrobe;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public Account? GetAccount(long accountId) {
            return Context.Account.Find(accountId);
        }

        public Account? GetAccount(string username) {
            return Context.Account
                .SingleOrDefault(account => account.Username == username);
        }

        public bool UpdateAccount(Account account, bool commit = false) {
            Context.Account.Update(account!);
            if (commit) {
                return Context.TrySaveChanges();
            }

            return true;
        }

        public (Account?, IList<Character>?) ListCharacters(long accountId) {
            Model.Account? model = Context.Account
                .Include(account => account.Characters)
                .SingleOrDefault(account => account.Id == accountId);

            return (model, model?.Characters.Select<Model.Character, Character>(c => c!).ToList());
        }

        //  If accountId is specified, only characters for the account will be returned.
        public Character? GetCharacter(long characterId, long accountId = -1) {
            if (accountId < 0) {
                return Context.Character.Find(characterId);
            }

            // Limit character fetching to those owned by account.
            return Context.Character.SingleOrDefault(character =>
                character.Id == characterId && character.AccountId == accountId);
        }

        public long GetCharacterId(string name) {
            return Context.Character.Where(character => character.Name == name)
                .Select(character => character.Id)
                .SingleOrDefault();
        }

        public CharacterInfo? GetCharacterInfo(long characterId) {
            return Context.Character.Where(character => character.Id == characterId)
                .Select<Model.Character, CharacterInfo>(character => character!)
                .SingleOrDefault();
        }

        public CharacterInfo? GetCharacterInfo(string name) {
            return Context.Character.Where(character => character.Name == name)
                .Select<Model.Character, CharacterInfo>(character => character!)
                .SingleOrDefault();
        }

        // We pass in objectId only for Player initialization.
        public Player? LoadPlayer(long accountId, long characterId, int objectId) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Model.Account? account = Context.Account.Find(accountId);
            if (account == null) {
                return null;
            }

            Model.Character? character = Context.Character.SingleOrDefault(character =>
                character.Id == characterId && character.AccountId == accountId);
            if (character == null) {
                return null;
            }

#if !DEBUG
            if (account.Online || character.Online) {
                throw new InvalidOperationException($"AlreadyOnline accountId:{accountId}, characterId:{characterId}");
            }
#endif

            account.Online = true;
            character.Online = true;
            Context.Account.Update(account);
            Context.Character.Update(character);
            Context.SaveChanges();

            Home? home = Context.Home.Include(home => home.Plot)
                .SingleOrDefault(home => home.AccountId == accountId);
            if (home == null) {
                return null;
            }

            var player = new Player(account!, character!, objectId) {
                Currency = new Currency{
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
                Unlock = Context.CharacterUnlock.Find(characterId)!,
                Home = home!,
            };

            if (home.Plot != null) {
                player.Home.Plot = ToPlot(home.Plot, layout: null);
            }

            return player;
        }

        public bool SavePlayer(Player player, bool logoff = false) {
            Console.WriteLine($"> Begin Save... {Context.ContextId}");
            Model.Account account = player.Account!;
            account.Currency = new AccountCurrency {
                Meret = player.Currency.Meret,
                GameMeret = player.Currency.GameMeret,
                MesoToken = player.Currency.MesoToken,
            };

            Model.Character character = player.Character!;
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

            if (logoff) {
                account.Online = false;
                character.Online = false;
            }

            Context.Update(account);
            Context.Update(character);

            CharacterUnlock unlock = player.Unlock!;
            unlock.CharacterId = character.Id;
            Context.Update(unlock);

            Context.ChangeTracker.Entries().DisplayStates();
            return Context.TrySaveChanges();
        }

        public (IList<KeyBind>? KeyBinds, IList<QuickSlot[]>? HotBars, List<SkillMacro>?, List<Wardrobe>?, IDictionary<StatAttribute, int>?, SkillBook?) LoadCharacterConfig(long characterId) {
            CharacterConfig? config = Context.CharacterConfig.Find(characterId);
            if (config == null) {
                return (null, null, null, null, null, null);
            }

            SkillBook? skillBook = config.SkillBook == null ? null : new SkillBook {
                MaxSkillTabs = config.SkillBook.MaxSkillTabs,
                ActiveSkillTabId = config.SkillBook.ActiveSkillTabId,
                SkillTabs = Context.SkillTab.Where(tab => tab.CharacterId == characterId)
                    .Select<Model.SkillTab, SkillTab>(tab => tab!)
                    .ToList(),
            };

            return (
                config.KeyBinds,
                config.HotBars,
                config.SkillMacros?.Select<Model.SkillMacro, SkillMacro>(macro => macro!).ToList(),
                config.Wardrobes?.Select<Model.Wardrobe, Wardrobe>(wardrobe => wardrobe!).ToList(),
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
                StatAttributes.PointAllocation allocation,
                SkillBook skillBook) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            CharacterConfig? config = Context.CharacterConfig.Find(characterId);
            if (config == null) {
                return false;
            }

            config.KeyBinds = keyBinds;
            config.HotBars = hotBars;
            config.SkillMacros = skillMacros.Select<SkillMacro, Model.SkillMacro>(macro => macro!).ToList();
            config.Wardrobes = wardrobes.Select<Wardrobe, Model.Wardrobe>(wardrobe => wardrobe!).ToList();
            config.StatAllocation = allocation.Attributes.ToDictionary(
                attribute => attribute,
                attribute => allocation[attribute]);
            config.SkillBook = new Model.SkillBook {
                MaxSkillTabs = skillBook.MaxSkillTabs,
                ActiveSkillTabId = skillBook.ActiveSkillTabId,
            };
            Context.CharacterConfig.Update(config);

            foreach (SkillTab skillTab in skillBook.SkillTabs) {
                Model.SkillTab model = skillTab!;
                model.CharacterId = characterId;
                Context.SkillTab.Update(model);
            }

            return Context.TrySaveChanges();
        }

        #region Create
        public Account? CreateAccount(Account account) {
            Model.Account model = account!;
            model.Id = 0;
            Context.Account.Add(model);
            Context.SaveChanges(); // Exception if failed.

            Context.Home.Add(new Home {
                AccountId = model.Id,
            });
            Context.SaveChanges(); // Exception if failed.

            return model;
        }

        public Character? CreateCharacter(Character character) {
            Model.Character model = character!;
            model.Id = 0;
            Context.Character.Add(model);
            return Context.TrySaveChanges() ? model : null;
        }

        public bool InitNewCharacter(long characterId, Unlock unlock) {
            CharacterUnlock model = unlock!;
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
            Model.SkillTab model = skillTab!;
            model.CharacterId = characterId;
            Context.SkillTab.Add(model);
            return Context.TrySaveChanges() ? model : null;
        }
        #endregion

        #region Delete
        public bool UpdateDelete(long accountId, long characterId, long time) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Model.Character? model = Context.Character.SingleOrDefault(character =>
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

            Model.Character? character = Context.Character.SingleOrDefault(character =>
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
