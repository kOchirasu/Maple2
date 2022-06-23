using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public IList<Buddy> ListBuddies(long characterId) {
            return JoinBuddyInfo(Context.Buddy.Where(buddy => buddy.CharacterId == characterId))
                .ToList();
        }

        public Buddy? GetBuddy(long id) {
            return JoinBuddyInfo(Context.Buddy.Where(buddy => buddy.Id == id))
                .SingleOrDefault();
        }

        public Buddy? GetBuddy(long ownerId, long buddyId) {
            return JoinBuddyInfo(Context.Buddy
                    .Where(buddy => buddy.CharacterId == ownerId && buddy.BuddyId == buddyId))
                .SingleOrDefault();
        }

        public BuddyType? GetBuddyType(long characterId, long buddyId) {
            return Context.Buddy.SingleOrDefault(buddy => buddy.CharacterId == characterId && buddy.BuddyId == buddyId)
                ?.Type;
        }

        public Buddy? CreateBuddy(long characterId, long buddyId, BuddyType type, string message = "") {
            var model = new Model.Buddy{
                CharacterId = characterId,
                BuddyId = buddyId,
                Type = type,
                Message = type != BuddyType.Blocked ? message : "",
                BlockMessage = type == BuddyType.Blocked ? message : "",
            };
            Context.Buddy.Add(model);

            bool success = Context.TrySaveChanges();
            return success ? GetBuddy(model.Id) : null;
        }

        public bool UpdateBuddy(Buddy buddy) {
            Context.Buddy.Update(buddy!);
            return Context.TrySaveChanges();
        }

        public bool RemoveBuddy(params Buddy[] buddies) {
            foreach (Buddy buddy in buddies) {
                Model.Buddy? model = buddy;
                if (model == null) {
                    continue;
                }

                Context.Buddy.Remove(model);
            }

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
