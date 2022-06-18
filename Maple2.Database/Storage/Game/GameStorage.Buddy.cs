using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public List<Buddy> ListBuddies(long characterId) {
            return JoinBuddyInfo(Context.Buddy.Where(buddy => buddy.CharacterId == characterId))
                .ToList();
        }

        public Buddy? GetBuddy(long id) {
            return JoinBuddyInfo(Context.Buddy.Where(buddy => buddy.Id == id))
                .SingleOrDefault();
        }

        public Buddy? CreateBuddy(Buddy buddy) {
            Model.Buddy model = buddy!;
            model.Id = 0;
            Context.Buddy.Add(model);

            bool success = Context.TrySaveChanges();
            return success ? GetBuddy(model.Id) : null;
        }

        public bool UpdateBuddy(Buddy buddy) {
            Context.Buddy.Update(buddy!);
            return Context.TrySaveChanges();
        }

        private IEnumerable<Buddy> JoinBuddyInfo(IQueryable<Model.Buddy> query) {
            return query.Include(buddy => buddy.BuddyCharacter)
                .Join(Context.Account,
                    buddy => buddy.BuddyCharacter.AccountId,
                    account => account.Id,
                    (buddy, account) => new Buddy(new PlayerInfo(
                        buddy.BuddyCharacter!, new HomeInfo("", 0, 0, 0, 0), account.Trophy)) {
                        Id = buddy.Id,
                        LastModified = buddy.LastModified,
                        Message = buddy.Message,
                        BlockMessage = buddy.BlockMessage,
                        Type = buddy.Type,
                    }
                );
        }
    }
}
