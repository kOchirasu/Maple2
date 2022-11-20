using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Sync;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Server.Game.Util.Sync;
using Maple2.Tools.Extensions;
using Serilog;
using static Maple2.Model.Error.BuddyError;

namespace Maple2.Server.Game.Manager;

public class BuddyManager : IDisposable {
    private readonly GameSession session;

    private readonly IDictionary<long, Buddy> buddies;
    private readonly IDictionary<long, Buddy> blocked;

    private bool disposed;
    private readonly ILogger logger = Log.Logger.ForContext<BuddyManager>();

    public BuddyManager(GameStorage.Request db, GameSession session) {
        this.session = session;
        IEnumerable<IGrouping<bool, BuddyEntry>> groups =
            db.ListBuddies(session.CharacterId).GroupBy(entry => entry.Type == BuddyType.Blocked);
        foreach (IGrouping<bool, BuddyEntry> group in groups) {
            Dictionary<long, Buddy> result = group
                .Select(entry => session.PlayerInfo.GetOrFetch(entry.BuddyId, out PlayerInfo? info) ? new Buddy(entry, info) : null)
                .WhereNotNull()
                .ToDictionary(entry => entry.Id, entry => entry);
            if (group.Key) {
                blocked = result;
            } else {
                buddies = result;
            }
        }

        // Set to empty dictionary if uninitialized by db.
        buddies ??= new Dictionary<long, Buddy>();
        blocked ??= new Dictionary<long, Buddy>();

        foreach ((long entryId, Buddy buddy) in buddies) {
            var listener = new PlayerInfoListener(UpdateField.Buddy, (type, info) => SyncUpdate(entryId, type, info));
            session.PlayerInfo.Listen(buddy.Info.CharacterId, listener);
        }
    }

    public void Dispose() {
        disposed = true;
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
            BuddyEntry? entry = db.CreateBuddy(session.CharacterId, receiverId, BuddyType.OutRequest, message);
            if (entry == null || !session.PlayerInfo.GetOrFetch(entry.BuddyId, out PlayerInfo? info)) {
                session.Send(BuddyPacket.Invite(error: s_buddy_err_miss_id));
                return;
            }

            var self = new Buddy(entry, info);
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
        BuddyEntry? entry = db.GetBuddy(session.CharacterId, senderId);
        if (entry == null || !session.PlayerInfo.GetOrFetch(entry.BuddyId, out PlayerInfo? info)) {
            logger.Debug("Could not find sender:{SenderId} for ReceiveInvite", senderId);
            return;
        }

        var self = new Buddy(entry, info);
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
        long receiverId = self.Info.CharacterId;
        BuddyEntry? entry = db.GetBuddy(receiverId, session.CharacterId);
        if (entry == null || !session.PlayerInfo.GetOrFetch(entry.BuddyId, out PlayerInfo? info)) {
            logger.Warning("Accept without paired entry");
            return;
        }

        var other = new Buddy(entry, info);
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
            self.Info.Channel = (short) response.Channel;

            session.Send(BuddyPacket.Accept(self));
            session.Send(BuddyPacket.UpdateInfo(self));
            if (self.Info.Online) {
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
        self.Info.Channel = 1; // We don't actually know the channel here, but we know they are online.
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
            long receiverId = self.Info.CharacterId;
            BuddyEntry? entry = db.GetBuddy(receiverId, session.CharacterId);
            if (entry == null || !session.PlayerInfo.GetOrFetch(entry.BuddyId, out PlayerInfo? info)) {
                logger.Warning("Decline without paired entry");
                return;
            }

            var other = new Buddy(entry, info);
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
                receiverId = self.Info.CharacterId;
                self.Type = BuddyType.Blocked;
                self.Message = message;
                db.UpdateBuddy(self);

                blocked[self.Id] = self;
                session.Send(BuddyPacket.Block(self.Id, self.Info.Name, self.Message));
                session.Send(BuddyPacket.UpdateInfo(self));
            } else {
                receiverId = db.GetCharacterId(name);
                if (receiverId == default) {
                    session.Send(BuddyPacket.Block(error: s_buddy_err_miss_id));
                    return;
                }

                BuddyEntry? selfEntry = db.CreateBuddy(session.CharacterId, receiverId, BuddyType.Blocked, message);
                if (selfEntry == null || !session.PlayerInfo.GetOrFetch(selfEntry.BuddyId, out PlayerInfo? selfInfo)) {
                    session.Send(BuddyPacket.Block(error: s_buddy_err_unknown));
                    return;
                }

                self = new Buddy(selfEntry, selfInfo);
                blocked[self.Id] = self;
                session.Send(BuddyPacket.Block(self.Id, self.Info.Name, self.Message));
                session.Send(BuddyPacket.Append(self));
            }

            // Delete entry for existing buddy if it exists.
            BuddyEntry? otherEntry = db.GetBuddy(receiverId, session.CharacterId);
            if (otherEntry == null || !session.PlayerInfo.GetOrFetch(otherEntry.BuddyId, out PlayerInfo? otherInfo)) {
                return;
            }

            var other = new Buddy(otherEntry, otherInfo);
            if (other.Type != BuddyType.Blocked && db.RemoveBuddy(other)) {
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
            long receiverId = self.Info.CharacterId;
            BuddyEntry? entry = db.GetBuddy(receiverId, session.CharacterId);
            if (entry == null || !session.PlayerInfo.GetOrFetch(entry.BuddyId, out PlayerInfo? otherInfo)) {
                logger.Warning("Delete without paired entry");
                return;
            }

            var other = new Buddy(entry, otherInfo);
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

            session.Send(BuddyPacket.UpdateBlock(self.Id, self.Info.Name, self.Message));
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
            long receiverId = self.Info.CharacterId;
            BuddyEntry? entry = db.GetBuddy(receiverId, session.CharacterId);
            if (entry == null || !session.PlayerInfo.GetOrFetch(entry.BuddyId, out PlayerInfo? otherInfo)) {
                logger.Warning("Cancel without paired entry");
                return;
            }

            var other = new Buddy(entry, otherInfo);
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
            if (buddyId == buddy.Info.CharacterId) {
                result = buddy;
                return true;
            }
        }
        foreach (Buddy buddy in blocked.Values) {
            if (buddyId == buddy.Info.CharacterId) {
                result = buddy;
                return true;
            }
        }

        result = null;
        return false;
    }

    private bool SyncUpdate(long id, UpdateField type, IPlayerInfo info) {
        if (disposed || !buddies.TryGetValue(id, out Buddy? buddy)) {
            return true;
        }

        bool wasOnline = buddy.Info.Online;
        buddy.Info.Update(type, info);

        session.Send(BuddyPacket.UpdateInfo(buddy));
        if (buddy.Info.Online != wasOnline) {
            session.Send(BuddyPacket.NotifyOnline(buddy));
        }
        return false;
    }
}
