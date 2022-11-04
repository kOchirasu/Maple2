using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Manager;

public sealed class MailManager {
    private const int BATCH_SIZE = 5;
    private const int FETCH_DELAY = 10;

    private readonly GameSession session;
    private DateTime lastFetch;
    private long lastReceivedMail;
    private int unreadMail;

    private readonly SortedList<long, Mail> inbox;

    public MailManager(GameSession session) {
        this.session = session;

        inbox = new SortedList<long, Mail>();
        Fetch();
    }

    public void Notify(bool force = false) {
        if (Fetch(force) || force) {
            session.Send(MailPacket.Notify(inbox.Count, unreadMail, unreadMail > 0));
        }
    }

    public void Load() {
        lock (inbox) {
            session.Send(MailPacket.StartList());
            foreach (ImmutableList<Mail> batch in inbox.Values.Batch(BATCH_SIZE)) {
                session.Send(MailPacket.Load(batch));
            }
            session.Send(MailPacket.EndList());
        }
    }

    public bool Delete(long mailId) {
        if (!inbox.ContainsKey(mailId)) {
            return false;
        }

        using GameStorage.Request db = session.GameStorage.Context();
        if (!db.DeleteMail(mailId, session.CharacterId)) {
            return false;
        }

        inbox.Remove(mailId);
        session.Send(MailPacket.Deleted(mailId));
        return true;
    }

    public MailError Read(long mailId) {
        if (!inbox.TryGetValue(mailId, out Mail? inboxMail)) {
            return MailError.mail_not_found;
        }

        using GameStorage.Request db = session.GameStorage.Context();
        Mail? mail = db.GetMail(mailId, session.CharacterId);
        if (mail == null) {
            return MailError.mail_not_found;
        }

        MailError error = ReadInternal(db, mail);
        if (error != MailError.none) {
            return error;
        }

        inboxMail.Update(mail);
        return MailError.none;
    }

    public MailError Collect(long mailId) {
        if (!inbox.TryGetValue(mailId, out Mail? inboxMail)) {
            return MailError.mail_not_found;
        }

        using GameStorage.Request db = session.GameStorage.Context();
        Mail? mail = db.GetMail(mailId, session.CharacterId);
        if (mail == null) {
            return MailError.mail_not_found;
        }

        lock (session.Item) {
            MailError error = CollectInternal(db, mail);
            if (error != MailError.none) {
                return error;
            }

            inboxMail.Update(mail);
            return MailError.none;
        }
    }

    private bool Fetch(bool force = false) {
        long characterId = session.CharacterId;

        lock (inbox) {
            if (!force && lastFetch.AddSeconds(FETCH_DELAY) > DateTime.Now) {
                return false;
            }

            bool newMail = false;
            using GameStorage.Request db = session.GameStorage.Context();
            foreach (Mail mail in db.GetReceivedMail(characterId, lastReceivedMail)) {
                if (mail.ReadTime == 0) {
                    unreadMail++;
                }

                newMail = true;
                inbox.Add(mail.Id, mail);
            }

            lastFetch = DateTime.Now;
            if (inbox.Count <= 0) {
                return false;
            }

            lastReceivedMail = inbox.Keys[inbox.Count - 1];
            return newMail;
        }
    }

    #region Internal (No locks)
    // ReadTime on input mail will be modified.
    private MailError ReadInternal(GameStorage.Request db, Mail mail) {
        if (mail.ReadTime > 0) {
            return MailError.s_mail_error_alreadyread;
        }

        Mail? readMail = db.MarkMailRead(mail.Id, session.CharacterId);
        if (readMail == null) {
            return MailError.s_mail_error;
        }

        mail.ReadTime = readMail.ReadTime;
        session.Send(MailPacket.Read(mail));
        return MailError.none;
    }

    // Items on input mail will be cleared.
    private MailError CollectInternal(GameStorage.Request db, Mail mail) {
        // Validate that collect is possible
        foreach (IGrouping<InventoryType, Item> group in mail.Items.GroupBy(item => item.Inventory)) {
            int requireSlots = group.Count();
            int freeSlots = session.Item.Inventory.FreeSlots(group.Key);

            if (requireSlots > freeSlots) {
                return MailError.s_err_job_inventory_full;
            }
        }

        // Collection is not possible is mail is already read...
        MailError error = ReadInternal(db, mail);
        if (error != MailError.none) {
            return error;
        }

        if (mail.MesoCollectTime == 0 && session.Currency.CanAddMeso(mail.Meso) == mail.Meso) {
            mail.MesoCollectTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            session.Currency.Meso += mail.Meso;
        }
        if (mail.MeretCollectTime == 0 && session.Currency.CanAddMeret(mail.Meret) == mail.Meret) {
            mail.MeretCollectTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            session.Currency.Meret += mail.Meret;
        }
        if (mail.GameMeretCollectTime == 0 && session.Currency.CanAddGameMeret(mail.GameMeret) == mail.GameMeret) {
            mail.GameMeretCollectTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            session.Currency.GameMeret += mail.GameMeret;
        }

        foreach (Item item in mail.Items) {
            if (!session.Item.Inventory.Add(item, notifyNew: true, commit: true)) {
                throw new InvalidOperationException($"Mail {mail.Id} was collected but items could not be added to inventory.");
            }

            session.Send(MailPacket.Collect(mail.Id));
            session.Send(MailPacket.CollectRead(mail));
        }
        mail.Items.Clear();

        return MailError.none;
    }
    #endregion
}
