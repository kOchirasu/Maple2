using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;

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

        public Account CreateAccount(Account account) {
            Model.Account model = account;
            model.Id = 0;
            context.Account.Add(model);
            return context.TrySaveChanges() ? model : null;
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

        public Character CreateCharacter(Character character) {
            Model.Character model = character;
            model.Id = 0;
            context.Character.Add(model);
            return context.TrySaveChanges() ? model : null;
        }

        // This does not commit the change, just stages the update.
        public void UpdateCharacter(Character character) {
            context.Character.Update(character);
        }

        public bool DeleteCharacter(long characterId, long accountId) {
            Model.Character character = context.Character.SingleOrDefault(character => 
                character.Id == characterId && character.AccountId == accountId);
            if (character == null) {
                return false;
            }

            context.Remove(character);
            return context.TrySaveChanges();
        }

        public Player LoadPlayer(long accountId, long characterId) {
            Model.Account account = context.Account.Find(accountId);
            if (account == null) {
                return null;
            }
            
            Model.Character character = context.Character.SingleOrDefault(character => 
                character.Id == characterId && character.AccountId == accountId);
            if (character == null) {
                return null;
            }
            
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

        public bool SavePlayer(Player player) {
            throw new NotImplementedException("cannot save player...");
        }
    }
}
