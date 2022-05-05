using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Database.Model;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Account = Maple2.Model.Game.Account;
using Character = Maple2.Model.Game.Character;

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
                .Select<Maple2.Database.Model.Character, CharacterInfo>(character => character)
                .SingleOrDefault();
        }

        public CharacterInfo GetCharacterInfo(string name) {
            return context.Character.Where(character => character.Name == name)
                .Select<Maple2.Database.Model.Character, CharacterInfo>(character => character)
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

            if (account.Online || character.Online) {
                throw new InvalidOperationException($"AlreadyOnline accountId:{accountId}, characterId:{characterId}");
            }

            account.Online = true;
            character.Online = true;
            context.Account.Update(account);
            context.Character.Update(character);
            context.SaveChanges();

            var player = new Player(account, character) {
                Currency = new Currency(
                    account.Currency.Meret,
                    account.Currency.GameMeret,
                    character.Currency.Meso,
                    character.Currency.EventMeret,
                    character.Currency.ValorToken,
                    character.Currency.Treva,
                    character.Currency.Rue,
                    character.Currency.HaviFruit,
                    character.Currency.ReverseCoin,
                    character.Currency.MentorToken,
                    character.Currency.MenteeToken,
                    character.Currency.StarPoint,
                    account.Currency.MesoToken),
                Unlock = (Unlock)context.CharacterUnlock.Find(characterId),
            };

            return player;
        }

        public bool SavePlayer(Player player, bool logoff = false) {
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

        public Unlock CreateUnlock(long characterId, Unlock unlock) {
            CharacterUnlock model = unlock;
            model.CharacterId = characterId;
            context.CharacterUnlock.Add(model);
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
