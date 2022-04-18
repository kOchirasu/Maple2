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
        
        public (Account, IList<Character>) ListCharacters(long accountId) {
            Model.Account model = context.Account
                .Include(account => account.Characters)
                .SingleOrDefault(account => account.Id == accountId);

            return (model, model?.Characters.Select<Model.Character, Character>(c => c).ToList());
        }

        public Character GetCharacter(long characterId) {
            return context.Character.Find(characterId);
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

        public bool DeleteCharacter(long characterId, bool force = false) {
            Model.Character character = context.Character.Find(characterId);
            if (character == null) {
                return false;
            }

            character.AccountId = 0;
            return context.TrySaveChanges();
        }
    }
}
