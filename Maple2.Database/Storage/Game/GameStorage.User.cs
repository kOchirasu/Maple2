using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Database.Model;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;
using Account = Maple2.Model.Game.Account;
using Character = Maple2.Model.Game.Character;
using SkillMacro = Maple2.Model.Game.SkillMacro;
using SkillBook = Maple2.Model.Game.SkillBook;
using SkillTab = Maple2.Model.Game.SkillTab;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public Account GetAccount(long accountId) {
            return context.Account.Find(accountId);
        }

        public Account GetAccount(string username) {
            return context.Account.AsQueryable()
                .SingleOrDefault(account => account.Username == username);
        }

        public bool UpdateAccount(Account account, bool commit = false) {
            context.Account.Update(account);
            if (commit) {
                return context.TrySaveChanges();
            }

            return true;
        }

        public (Account, IList<Character>) ListCharacters(long accountId) {
            Model.Account model = context.Account
                .Include(account => account.Characters)
                .SingleOrDefault(account => account.Id == accountId);

            return (model, model?.Characters.Select<Model.Character, Character>(c => c).ToList());
        }

        //  If accountId is specified, only characters for the account will be returned.
        public Character GetCharacter(long characterId, long accountId = -1) {
            if (accountId < 0) {
                return context.Character.Find(characterId);
            }

            // Limit character fetching to those owned by account.
            return context.Character.SingleOrDefault(character =>
                character.Id == characterId && character.AccountId == accountId);
        }

        public CharacterInfo GetCharacterInfo(long characterId) {
            return context.Character.Where(character => character.Id == characterId)
                .Select<Model.Character, CharacterInfo>(character => character)
                .SingleOrDefault();
        }

        public CharacterInfo GetCharacterInfo(string name) {
            return context.Character.Where(character => character.Name == name)
                .Select<Model.Character, CharacterInfo>(character => character)
                .SingleOrDefault();
        }

        public Player LoadPlayer(long accountId, long characterId) {
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Model.Account account = context.Account.Find(accountId);
            if (account == null) {
                return null;
            }

            Model.Character character = context.Character.SingleOrDefault(character =>
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
            context.Account.Update(account);
            context.Character.Update(character);
            context.SaveChanges();

            var player = new Player(account, character) {
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
                Unlock = (Unlock)context.CharacterUnlock.Find(characterId),
            };

            return player;
        }

        public bool SavePlayer(Player player, bool logoff = false) {
            Console.WriteLine($"> Begin Save... {context.ContextId}");
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

            if (logoff) {
                account.Online = false;
                character.Online = false;
            }

            context.Update(account);
            context.Update(character);

            CharacterUnlock unlock = player.Unlock;
            unlock.CharacterId = character.Id;
            context.Update(unlock);

            context.ChangeTracker.Entries().DisplayStates();
            return context.TrySaveChanges();
        }

        public (IList<KeyBind> KeyBinds, IList<QuickSlot[]> HotBars, List<SkillMacro>, SkillBook) LoadCharacterConfig(long characterId) {
            CharacterConfig config = context.CharacterConfig.Find(characterId);
            if (config == null) {
                return (null, null, null, null);
            }

            var skillBook = new SkillBook {
                MaxSkillTabs = config.SkillBook.MaxSkillTabs,
                ActiveSkillTabId = config.SkillBook.ActiveSkillTabId,
                SkillTabs = context.SkillTab.Where(tab => tab.CharacterId == characterId)
                    .Select<Model.SkillTab, SkillTab>(tab => tab)
                    .ToList(),
            };

            return (
                config.KeyBinds,
                config.HotBars,
                config.SkillMacros?.Select<Model.SkillMacro, SkillMacro>(macro => macro).ToList(),
                skillBook
            );
        }

        public bool SaveCharacterConfig(long characterId, IList<KeyBind> keyBinds, IList<QuickSlot[]> hotBars,
                IEnumerable<SkillMacro> skillMacros, SkillBook skillBook) {
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            CharacterConfig config = context.CharacterConfig.Find(characterId);
            if (config == null) {
                return false;
            }

            config.KeyBinds = keyBinds;
            config.HotBars = hotBars;
            config.SkillMacros = skillMacros.Select<SkillMacro, Model.SkillMacro>(macro => macro).ToList();
            config.SkillBook = new Model.SkillBook {
                MaxSkillTabs = skillBook.MaxSkillTabs,
                ActiveSkillTabId = skillBook.ActiveSkillTabId,
            };
            context.CharacterConfig.Update(config);

            foreach (SkillTab skillTab in skillBook.SkillTabs) {
                Model.SkillTab model = skillTab;
                model.CharacterId = characterId;
                context.SkillTab.Update(model);
            }

            return context.TrySaveChanges();
        }

        #region Create
        public Account CreateAccount(Account account) {
            Model.Account model = account;
            model.Id = 0;
            context.Account.Add(model);
            return context.TrySaveChanges() ? model : null;
        }

        public Character CreateCharacter(Character character) {
            Model.Character model = character;
            model.Id = 0;
            context.Character.Add(model);
            return context.TrySaveChanges() ? model : null;
        }

        public bool InitNewCharacter(long characterId, Unlock unlock) {
            CharacterUnlock model = unlock;
            model.CharacterId = characterId;
            context.CharacterUnlock.Add(model);

            SkillTab defaultTab = CreateSkillTab(characterId, new SkillTab("Build 1") {Id = characterId});
            var config = new CharacterConfig {
                CharacterId = characterId,
                SkillBook = new Model.SkillBook {
                    MaxSkillTabs = 1,
                    ActiveSkillTabId = defaultTab.Id,
                },
            };
            context.CharacterConfig.Add(config);

            return context.TrySaveChanges();
        }

        public SkillTab CreateSkillTab(long characterId, SkillTab skillTab) {
            Model.SkillTab model = skillTab;
            model.CharacterId = characterId;
            context.SkillTab.Add(model);
            return context.TrySaveChanges() ? model : null;
        }
        #endregion

        #region Delete
        public bool UpdateDelete(long accountId, long characterId, long time) {
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Model.Character model = context.Character.SingleOrDefault(character =>
                character.Id == characterId && character.AccountId == accountId);
            if (model == null) {
                return false;
            }

            model.DeleteTime = time.FromEpochSeconds();
            context.Update(model);
            return context.TrySaveChanges();
        }

        public bool DeleteCharacter(long accountId, long characterId) {
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Model.Character character = context.Character.SingleOrDefault(character =>
                character.Id == characterId && character.AccountId == accountId);
            if (character == null) {
                return false;
            }

            context.Remove(character);
            return context.TrySaveChanges();
        }
        #endregion
    }
}
