using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Data;
using Maple2.Database.Extensions;
using Maple2.Model.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maple2.Database.Storage;

public partial class UserStorage {
    private readonly DbContextOptions options;
    private readonly ILogger logger;

    public UserStorage(DbContextOptions options, ILogger<UserStorage> logger) {
        this.options = options;
        this.logger = logger;
    }

    public Request Context()  {
        return new Request(this, new Ms2Context(options), logger);
    }

    public partial class Request : DatabaseRequest<Ms2Context> {
        private readonly UserStorage storage;

        public Request(UserStorage storage, Ms2Context context, ILogger logger) : base(context, logger) {
            this.storage = storage;
        }

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
        
        public List<Character> ListCharacters(long accountId) {
            return context.Character.AsQueryable()
                .Where(character => character.AccountId == accountId)
                .AsEnumerable()
                .Select(character => (Character) character)
                .ToList();
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
