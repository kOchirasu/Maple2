using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Serilog;
using static Maple2.Model.Error.BuddyError;

namespace Maple2.Server.Game.Manager;

public class BuddyManager {
    private readonly GameSession session;

    public readonly IDictionary<long, Buddy> Buddies;
    //public readonly IDictionary<long, Buddy> Blocked;

    private readonly ILogger logger = Log.Logger.ForContext<BuddyManager>();

    public BuddyManager(GameStorage.Request db, GameSession session) {
        this.session = session;
        Buddies = db.ListBuddies(session.CharacterId)
            .ToDictionary(entry => entry.Id, entry => entry);
    }

    public void Load() {
        session.Send(BuddyPacket.StartList());
        if (Buddies.Count > 0) {
            session.Send(BuddyPacket.Load(Buddies.Values));
        }
        session.Send(BuddyPacket.EndList(Buddies.Count));
    }

    public void SendInvite(string name, string message) {
        if (name == session.Player.Value.Character.Name) {
            session.Send(BuddyPacket.Invite(error: s_buddy_err_my_id_ex));
            return;
        }

        using GameStorage.Request db = session.GameStorage.Context();
        long receiverId = db.GetCharacterId(name);
        if (receiverId == default) {
            session.Send(BuddyPacket.Invite(error: s_buddy_err_miss_id));
            return;
        }

        BuddyError error = db.GetBuddyType(receiverId, session.CharacterId) switch {
            BuddyType.Default => s_buddy_err_already_buddy,
            BuddyType.InRequest => s_buddy_err_already_request_somebody,
            BuddyType.OutRequest => s_buddy_err_request_somebody,
            BuddyType.Blocked => s_buddy_refused_request_from_somebody,
            _ => ok,
        };
        if (error != ok) {
            session.Send(BuddyPacket.Invite(name, error: error));
            return;
        }

        try {
            db.BeginTransaction();
            Buddy self = db.CreateBuddy(session.CharacterId, receiverId, BuddyType.OutRequest, message)!;
            db.CreateBuddy(receiverId, session.CharacterId, BuddyType.InRequest, message);
            db.Commit();

            Buddies[self.Id] = self;

            session.Send(BuddyPacket.Invite(name, message));
            session.Send(BuddyPacket.Append(self));

            BuddyResponse _ = session.World.Buddy(new BuddyRequest {
                ReceiverId = receiverId,
                Invite = new BuddyRequest.Types.Invite {SenderId = session.CharacterId},
            });
        } catch (SystemException ex) {
            logger.Warning(ex, "Buddy Invite failed.");
            session.Send(BuddyPacket.Invite(error: s_buddy_err_unknown));
        }
    }

    public void ReceiveInvite(long senderId) {
        using GameStorage.Request db = session.GameStorage.Context();
        Buddy? buddy = db.GetBuddy(session.CharacterId, senderId);
        if (buddy == null) {
            return;
        }

        // DB written by sender as single transaction.
        Buddies[buddy.Id] = buddy;
        session.Send(BuddyPacket.Append(buddy));
    }

    public void SendAccept(long entryId) {
        if (!Buddies.TryGetValue(entryId, out Buddy? self) || self.Type != BuddyType.InRequest) {
            return;
        }

        using GameStorage.Request db = session.GameStorage.Context();
        long receiverId = self.BuddyInfo.CharacterId;
        Buddy? other = db.GetBuddy(receiverId, session.CharacterId);
        if (other == null) {
            logger.Warning("Buddy Accept without paired entry.");
            return;
        }
        self.Type = BuddyType.Default;
        other.Type = BuddyType.Default;

        try {
            db.BeginTransaction();
            db.UpdateBuddy(self);
            db.UpdateBuddy(other);
            db.Commit();

            session.Send(BuddyPacket.Accept(self));
            session.Send(BuddyPacket.UpdateInfo(self));

            BuddyResponse response = session.World.Buddy(new BuddyRequest {
                ReceiverId = receiverId,
                Accept = new BuddyRequest.Types.Accept {EntryId = other.Id},
            });
            if (response.Online) {
                self.BuddyInfo.Character.Online = true;
                session.Send(BuddyPacket.NotifyOnline(self));
            }
        } catch (SystemException ex) {
            logger.Warning(ex, "Buddy Accept failed.");
        }
    }

    public void ReceiveAccept(long entryId) {
        if (!Buddies.TryGetValue(entryId, out Buddy? self)) {
            return;
        }

        // DB written by sender as single transaction.
        self.Type = BuddyType.Default;
        session.Send(BuddyPacket.UpdateInfo(self));
        session.Send(BuddyPacket.NotifyAccept(self));
    }

    public void SendDecline(long entryId) {
        if (!Buddies.TryGetValue(entryId, out Buddy? self) || self.Type != BuddyType.InRequest) {
            return;
        }

        using GameStorage.Request db = session.GameStorage.Context();
        long receiverId = self.BuddyInfo.CharacterId;
        Buddy? other = db.GetBuddy(receiverId, session.CharacterId);
        if (other == null) {
            logger.Warning("Buddy Decline without paired entry.");
            return;
        }

        Buddies.Remove(entryId);

        try {
            db.BeginTransaction();
            db.RemoveBuddy(self, other);
            db.Commit();

            session.Send(BuddyPacket.Decline(self));
            BuddyResponse _ = session.World.Buddy(new BuddyRequest {
                ReceiverId = receiverId,
                Decline = new BuddyRequest.Types.Decline {EntryId = other.Id},
            });
        } catch (SystemException ex) {
            logger.Warning(ex, "Buddy Decline failed.");
        }
    }

    public void ReceiveDecline(long entryId) {
        // DB written by sender as single transaction.
        if (!Buddies.Remove(entryId, out Buddy? self)) {
            return;
        }

        session.Send(BuddyPacket.Delete(self));
    }

    public void SendBlock(long buddyId, string message) {

    }

    public void ReceiveBlock() {

    }

    public void SendUnblock(long entryId) {
        if (!Buddies.TryGetValue(entryId, out Buddy? self) || self.Type != BuddyType.Blocked) {
            return;
        }
    }

    public void ReceiveUnblock() {

    }

    public void CancelRequest(long entryId) {
        if (!Buddies.TryGetValue(entryId, out Buddy? self) || self.Type != BuddyType.OutRequest) {
            return;
        }
    }

    public void Delete(long entryId) {
        if (!Buddies.TryGetValue(entryId, out Buddy? self)) {
            return;
        }
    }

    public void UpdateBlock(long entryId, string message) {
        if (!Buddies.TryGetValue(entryId, out Buddy? self) || self.Type != BuddyType.Blocked) {
            return;
        }
    }
}
