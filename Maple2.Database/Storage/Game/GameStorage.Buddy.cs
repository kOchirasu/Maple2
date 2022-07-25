using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Buddy = Maple2.Model.Game.Buddy;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public IList<Buddy> ListBuddies(long ownerId) {
            return (from buddy in Context.Buddy where buddy.Id == ownerId
                    join account in Context.Account on buddy.BuddyCharacter.AccountId equals account.Id
                    join indoor in Context.UgcMap on
                        new {OwnerId=buddy.BuddyCharacter.AccountId, Indoor=true} equals new {indoor.OwnerId, indoor.Indoor}
                    join outdoor in Context.UgcMap on
                        new {OwnerId=buddy.BuddyCharacter.AccountId, Indoor=true} equals new {outdoor.OwnerId, outdoor.Indoor} into plot
                    from outdoor in plot.DefaultIfEmpty()
                    select new Buddy(BuildPlayerInfo(buddy.BuddyCharacter, indoor, outdoor, account.Trophy)) {
                        Id = buddy.Id,
                        OwnerId = buddy.OwnerId,
                        LastModified = buddy.LastModified.ToEpochSeconds(),
                        Message = buddy.Message,
                        Type = buddy.Type,
                    }).ToList();
        }

        public Buddy? GetBuddy(long id) {
            return (from buddy in Context.Buddy where buddy.Id == id
                    join account in Context.Account on buddy.BuddyCharacter.AccountId equals account.Id
                    join indoor in Context.UgcMap on
                        new {OwnerId=buddy.BuddyCharacter.AccountId, Indoor=true} equals new {indoor.OwnerId, indoor.Indoor}
                    join outdoor in Context.UgcMap on
                        new {OwnerId=buddy.BuddyCharacter.AccountId, Indoor=true} equals new {outdoor.OwnerId, outdoor.Indoor} into plot
                    from outdoor in plot.DefaultIfEmpty()
                    select new Buddy(BuildPlayerInfo(buddy.BuddyCharacter, indoor, outdoor, account.Trophy)) {
                        Id = buddy.Id,
                        OwnerId = buddy.OwnerId,
                        LastModified = buddy.LastModified.ToEpochSeconds(),
                        Message = buddy.Message,
                        Type = buddy.Type,
                    }).FirstOrDefault();
        }

        public Buddy? GetBuddy(long ownerId, long buddyId) {
            return (from buddy in Context.Buddy where buddy.Id == buddyId && buddy.OwnerId == ownerId
                    join account in Context.Account on buddy.BuddyCharacter.AccountId equals account.Id
                    join indoor in Context.UgcMap on
                        new {OwnerId=buddy.BuddyCharacter.AccountId, Indoor=true} equals new {indoor.OwnerId, indoor.Indoor}
                    join outdoor in Context.UgcMap on
                        new {OwnerId=buddy.BuddyCharacter.AccountId, Indoor=true} equals new {outdoor.OwnerId, outdoor.Indoor} into plot
                    from outdoor in plot.DefaultIfEmpty()
                    select new Buddy(BuildPlayerInfo(buddy.BuddyCharacter, indoor, outdoor, account.Trophy)) {
                        Id = buddy.Id,
                        OwnerId = buddy.OwnerId,
                        LastModified = buddy.LastModified.ToEpochSeconds(),
                        Message = buddy.Message,
                        Type = buddy.Type,
                    }).FirstOrDefault();
        }

        public BuddyType? GetBuddyType(long ownerId, long buddyId) {
            return Context.Buddy.FirstOrDefault(buddy => buddy.OwnerId == ownerId && buddy.BuddyId == buddyId)?.Type;
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
    }
}
