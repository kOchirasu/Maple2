using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public IList<Buddy> ListBuddies(long ownerId) {
            return JoinBuddyInfo(Context.Buddy.Where(buddy => buddy.OwnerId == ownerId))
                .ToList();
        }

        public Buddy? GetBuddy(long id) {
            return JoinBuddyInfo(Context.Buddy.Where(buddy => buddy.Id == id))
                .SingleOrDefault();
        }

        public Buddy? GetBuddy(long ownerId, long buddyId) {
            return JoinBuddyInfo(Context.Buddy
                    .Where(buddy => buddy.OwnerId == ownerId && buddy.BuddyId == buddyId))
                .SingleOrDefault();
        }

        public BuddyType? GetBuddyType(long ownerId, long buddyId) {
            return Context.Buddy.SingleOrDefault(buddy => buddy.OwnerId == ownerId && buddy.BuddyId == buddyId)?.Type;
        }

        public Buddy? CreateBuddy(long ownerId, long buddyId, BuddyType type, string message = "") {
            var model = new Model.Buddy{
                OwnerId = ownerId,
                BuddyId = buddyId,
                Type = type,
                Message = message,
            };
            Context.Buddy.Add(model);

            bool success = Context.TrySaveChanges();
            return success ? GetBuddy(model.Id) : null;
        }

        public int CountBuddy(long ownerId) {
            return Context.Buddy.Count(buddy => buddy.OwnerId == ownerId && buddy.Type != BuddyType.Blocked);
        }

        public bool UpdateBuddy(params Buddy[] buddies) {
            foreach (Buddy buddy in buddies) {
                Context.Buddy.Update(buddy!);
            }

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
                    (buddy, account) => new Buddy(new PlayerInfo(buddy.BuddyCharacter!, account.Home, account.Trophy)) {
                        Id = buddy.Id,
                        OwnerId = buddy.OwnerId,
                        LastModified = buddy.LastModified.ToEpochSeconds(),
                        Message = buddy.Message,
                        Type = buddy.Type,
                    }
                );
        }
    }
}
