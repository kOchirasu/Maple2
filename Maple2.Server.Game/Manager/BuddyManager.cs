using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Serilog;
using static Maple2.Model.Error.BuddyError;

namespace Maple2.Server.Game.Manager;

public class BuddyManager {
    private readonly GameSession session;

    private readonly IDictionary<long, Buddy> buddies;
    private readonly IDictionary<long, Buddy> blocked;

    private readonly ILogger logger = Log.Logger.ForContext<BuddyManager>();

    public BuddyManager(GameStorage.Request db, GameSession session) {
        this.session = session;
        IEnumerable<IGrouping<bool, Buddy>> groups =
            db.ListBuddies(session.CharacterId).GroupBy(entry => entry.Type == BuddyType.Blocked);
        foreach (IGrouping<bool, Buddy> group in groups) {
            if (group.Key) {
                blocked = group.ToDictionary(entry => entry.Id, entry => entry);
            } else {
                buddies = group.ToDictionary(entry => entry.Id, entry => entry);
            }
        }

        // Set to empty dictionary if uninitialized by db.
        buddies ??= new Dictionary<long, Buddy>();
        blocked ??= new Dictionary<long, Buddy>();
    }

    public void Load() {
        session.Send(BuddyPacket.StartList());
        if (buddies.Count + blocked.Count > 0) {
            session.Send(BuddyPacket.Load(buddies.Values, blocked.Values));
        }
        session.Send(BuddyPacket.EndList(buddies.Count));
    }

    public void SendInvite(string name, string message) {
        if (name.Length > Constant.CharacterNameLengthMax || message.Length > Constant.BuddyMessageLengthMax) {
            session.Send(BuddyPacket.Invite(error: s_buddy_err_unknown));
            return;
        }
        if (name == session.Player.Value.Character.Name) {
            session.Send(BuddyPacket.Invite(error: s_buddy_err_my_id_ex));
            return;
        }
        if (buddies.Count >= Constant.MaxBuddyCount) {
            session.Send(BuddyPacket.Invite(error: s_buddy_err_max_buddy));
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
            if (db.CountBuddy(receiverId) >= Constant.MaxBuddyCount) {
                session.Send(BuddyPacket.Invite(name: name, error: s_buddy_err_target_full));
                return;
            }
            Buddy self = db.CreateBuddy(session.CharacterId, receiverId, BuddyType.OutRequest, message)!;
            db.CreateBuddy(receiverId, session.CharacterId, BuddyType.InRequest, message);
            db.Commit();

            buddies[self.Id] = self;

            session.Send(BuddyPacket.Invite(name, message));
            session.Send(BuddyPacket.Append(self));

            BuddyResponse _ = session.World.Buddy(new BuddyRequest {
                ReceiverId = receiverId,
                Invite = new BuddyRequest.Types.Invite {SenderId = session.CharacterId},
            });
        } catch (SystemException ex) {
            logger.Warning(ex, "Invite failed");
            session.Send(BuddyPacket.Invite(error: s_buddy_err_unknown));
        }
    }

    public void ReceiveInvite(long senderId) {
        using GameStorage.Request db = session.GameStorage.Context();
        Buddy? self = db.GetBuddy(session.CharacterId, senderId);
        if (self == null) {
            logger.Debug("Could not find sender:{SenderId} for ReceiveInvite", senderId);
            return;
        }

        // DB written by sender as single transaction.
        buddies[self.Id] = self;
        session.Send(BuddyPacket.Append(self));
    }

    public void SendAccept(long entryId) {
        if (!buddies.TryGetValue(entryId, out Buddy? self) || self.Type != BuddyType.InRequest) {
            logger.Debug("Could not find entry:{EntryId} to Accept", entryId);
            return;
        }

        using GameStorage.Request db = session.GameStorage.Context();
        long receiverId = self.BuddyInfo.CharacterId;
        Buddy? other = db.GetBuddy(receiverId, session.CharacterId);
        if (other == null) {
            logger.Warning("Accept without paired entry");
            return;
        }
        self.Type = BuddyType.Default;
        other.Type = BuddyType.Default;

        try {
            if (!db.UpdateBuddy(self, other)) {
                logger.Error("Failed to update buddy");
                return;
            }

            BuddyResponse response = session.World.Buddy(new BuddyRequest {
                ReceiverId = receiverId,
                Accept = new BuddyRequest.Types.Accept {EntryId = other.Id},
            });
            self.BuddyInfo.Character.Online = response.Online;

            session.Send(BuddyPacket.Accept(self));
            session.Send(BuddyPacket.UpdateInfo(self));
            if (self.BuddyInfo.Character.Online) {
                session.Send(BuddyPacket.NotifyOnline(self));
            }
        } catch (SystemException ex) {
            logger.Warning(ex, "Buddy Accept failed");
        }
    }

    public void ReceiveAccept(long entryId) {
        if (!buddies.TryGetValue(entryId, out Buddy? self)) {
            logger.Debug("Could not find entry:{EntryId} for ReceiveAccept", entryId);
            return;
        }

        // DB written by sender as single transaction.
        self.Type = BuddyType.Default;
        self.BuddyInfo.Character.Online = true;
        session.Send(BuddyPacket.UpdateInfo(self));
        session.Send(BuddyPacket.NotifyAccept(self));
        session.Send(BuddyPacket.NotifyOnline(self));
    }

    public void SendDecline(long entryId) {
        if (!buddies.TryGetValue(entryId, out Buddy? self) || self.Type != BuddyType.InRequest) {
            logger.Debug("Could not find entry:{EntryId} to Decline", entryId);
            return;
        }

        try {
            using GameStorage.Request db = session.GameStorage.Context();
            long receiverId = self.BuddyInfo.CharacterId;
            Buddy? other = db.GetBuddy(receiverId, session.CharacterId);
            if (other == null) {
                logger.Warning("Decline without paired entry");
                return;
            }

            if (!buddies.Remove(entryId) || !db.RemoveBuddy(self, other)) {
                return;
            }

            session.Send(BuddyPacket.Decline(self));
            BuddyResponse _ = session.World.Buddy(new BuddyRequest {
                ReceiverId = receiverId,
                Decline = new BuddyRequest.Types.Decline {EntryId = other.Id},
            });
        } catch (SystemException ex) {
            logger.Warning(ex, "Decline failed");
        }
    }

    public void ReceiveDecline(long entryId) {
        // DB written by sender as single transaction.
        if (!buddies.Remove(entryId, out Buddy? self)) {
            logger.Debug("Could not find entry:{EntryId} for ReceiveDecline", entryId);
            return;
        }

        session.Send(BuddyPacket.Remove(self));
    }

    public void SendBlock(long entryId, string name, string message) {
        if (name.Length > Constant.CharacterNameLengthMax || message.Length > Constant.BuddyMessageLengthMax) {
            session.Send(BuddyPacket.Block(error: s_buddy_err_unknown));
            return;
        }
        if (blocked.Count >= Constant.MaxBlockCount) {
            session.Send(BuddyPacket.Block(name: name, error: s_buddy_err_max_block));
            return;
        }
        if (blocked.TryGetValue(entryId, out _)) {
            logger.Information("Could not find entry:{EntryId} to Block", entryId);
            return;
        }

        try {
            using GameStorage.Request db = session.GameStorage.Context();
            logger.Warning("EntryId: {Buddy}", entryId);
            long receiverId;
            if (buddies.Remove(entryId, out Buddy? self)) {
                receiverId = self.BuddyInfo.CharacterId;
                self.Type = BuddyType.Blocked;
                self.Message = message;
                db.UpdateBuddy(self);

                blocked[self.Id] = self;
                session.Send(BuddyPacket.Block(self.Id, self.BuddyInfo.Name, self.Message));
                session.Send(BuddyPacket.UpdateInfo(self));
            } else {
                receiverId = db.GetCharacterId(name);
                if (receiverId == default) {
                    session.Send(BuddyPacket.Block(error: s_buddy_err_miss_id));
                    return;
                }

                self = db.CreateBuddy(session.CharacterId, receiverId, BuddyType.Blocked, message);
                if (self == null) {
                    session.Send(BuddyPacket.Block(error: s_buddy_err_unknown));
                    return;
                }

                blocked[self.Id] = self;
                session.Send(BuddyPacket.Block(self.Id, self.BuddyInfo.Name, self.Message));
                session.Send(BuddyPacket.Append(self));
            }

            // Delete entry for existing buddy if it exists.
            Buddy? other = db.GetBuddy(receiverId, session.CharacterId);
            if (other != null && other.Type != BuddyType.Blocked && db.RemoveBuddy(other)) {
                BuddyResponse _ = session.World.Buddy(new BuddyRequest {
                    ReceiverId = receiverId,
                    Block = new BuddyRequest.Types.Block {SenderId = session.CharacterId},
                });
            }
        } catch (SystemException ex) {
            logger.Warning(ex, "Block failed");
            session.Send(BuddyPacket.Block(error: s_buddy_err_unknown));
        }
    }

    public void ReceiveBlock(long senderId) {
        if (!TryGet(senderId, out Buddy? buddy)) {
            logger.Debug("Could not find sender:{SenderId} for ReceiveBlock", senderId);
            return;
        }

        if (buddies.Remove(buddy.Id)) {
            session.Send(BuddyPacket.Remove(buddy));
        }
    }

    public void Unblock(long entryId) {
        if (!blocked.Remove(entryId, out Buddy? self)) {
            logger.Debug("Could not find entry:{EntryId} to Unblock", entryId);
            return;
        }

        try {
            using GameStorage.Request db = session.GameStorage.Context();
            if (!db.RemoveBuddy(self)) {
                return;
            }

            session.Send(BuddyPacket.Unblock(self));
            session.Send(BuddyPacket.Remove(self));
        } catch (SystemException ex) {
            logger.Warning(ex, "Unblock failed");
        }
    }

    public void SendDelete(long entryId) {
        if (!buddies.TryGetValue(entryId, out Buddy? self) || self.Type != BuddyType.Default) {
            logger.Debug("Could not find entry:{EntryId} to Delete", entryId);
            return;
        }

        try {
            using GameStorage.Request db = session.GameStorage.Context();
            long receiverId = self.BuddyInfo.CharacterId;
            Buddy? other = db.GetBuddy(receiverId, session.CharacterId);
            if (other == null) {
                logger.Warning("Delete without paired entry");
                return;
            }

            if (!db.RemoveBuddy(self, other)) {
                return;
            }

            session.Send(BuddyPacket.Remove(self));
            BuddyResponse _ = session.World.Buddy(new BuddyRequest {
                ReceiverId = receiverId,
                Delete = new BuddyRequest.Types.Delete {EntryId = other.Id},
            });
        } catch (SystemException ex) {
            logger.Warning(ex, "Delete failed");
        }
    }

    public void ReceiveDelete(long entryId) {
        // DB written by sender as single transaction.
        if (!buddies.Remove(entryId, out Buddy? self)) {
            logger.Debug("Could not find entry:{EntryId} for ReceiveDelete", entryId);
            return;
        }

        session.Send(BuddyPacket.Remove(self));
    }

    public void UpdateBlock(long entryId, string name, string message) {
        if (name.Length > Constant.CharacterNameLengthMax || message.Length > Constant.BuddyMessageLengthMax) {
            session.Send(BuddyPacket.UpdateBlock(error: s_buddy_err_unknown));
            return;
        }
        if (!blocked.TryGetValue(entryId, out Buddy? self)) {
            session.Send(BuddyPacket.UpdateBlock(error: s_buddy_err_unknown));
            return;
        }

        try {
            using GameStorage.Request db = session.GameStorage.Context();
            self.Message = message;
            if (!db.UpdateBuddy(self)) {
                return;
            }

            session.Send(BuddyPacket.UpdateBlock(self.Id, self.BuddyInfo.Name, self.Message));
        } catch (SystemException ex) {
            logger.Warning(ex, "UpdateBlock failed");
            session.Send(BuddyPacket.UpdateBlock(error: s_buddy_err_unknown));
        }
    }

    public void SendCancel(long entryId) {
        if (!buddies.TryGetValue(entryId, out Buddy? self) || self.Type != BuddyType.OutRequest) {
            logger.Debug("Could not find entry:{EntryId} to Cancel", entryId);
            return;
        }

        try {
            using GameStorage.Request db = session.GameStorage.Context();
            long receiverId = self.BuddyInfo.CharacterId;
            Buddy? other = db.GetBuddy(receiverId, session.CharacterId);
            if (other == null) {
                logger.Warning("Cancel without paired entry");
                return;
            }

            if (!db.RemoveBuddy(self, other)) {
                return;
            }

            session.Send(BuddyPacket.Cancel(self));
            BuddyResponse _ = session.World.Buddy(new BuddyRequest {
                ReceiverId = receiverId,
                Cancel = new BuddyRequest.Types.Cancel {EntryId = other.Id},
            });
        } catch (SystemException ex) {
            logger.Warning(ex, "Cancel failed");
        }
    }

    public void ReceiveCancel(long entryId) {
        // DB written by sender as single transaction.
        if (!buddies.Remove(entryId, out Buddy? self)) {
            logger.Debug("Could not find entry:{EntryId} for ReceiveCancel", entryId);
            return;
        }

        session.Send(BuddyPacket.Remove(self));
    }

    private bool TryGet(long buddyId, [NotNullWhen(true)] out Buddy? result) {
        foreach (Buddy buddy in buddies.Values) {
            if (buddyId == buddy.BuddyInfo.CharacterId) {
                result = buddy;
                return true;
            }
        }
        foreach (Buddy buddy in blocked.Values) {
            if (buddyId == buddy.BuddyInfo.CharacterId) {
                result = buddy;
                return true;
            }
        }

        result = null;
        return false;
    }
}
