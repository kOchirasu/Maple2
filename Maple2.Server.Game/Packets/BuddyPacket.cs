using System.Collections.Generic;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class BuddyPacket {
    private enum Command : byte {
        Load = 1,
        Invite = 2,
        Accept = 3,
        Decline = 4,
        Block = 5,
        Unblock = 6,
        Delete = 7,
        UpdateInfo = 8,
        Append = 9,
        UpdateBlock = 10,
        NotifyAccept = 11,
        NotifyBlock = 12,
        NotifyDelete = 13,
        NotifyOnline = 14,
        StartList = 15,
        Cancel = 17,
        Forbidden = 18,
        EndList = 19,
        Unknown = 20,
    }

    public static ByteWriter Load(ICollection<Buddy> buddies) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.Load);
        pWriter.WriteInt(buddies.Count);
        foreach (Buddy buddy in buddies) {
            pWriter.WriteClass<Buddy>(buddy);
        }

        return pWriter;
    }

    public static ByteWriter Invite(string name = "", string message = "", BuddyError error = BuddyError.ok) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.Invite);
        pWriter.Write<BuddyError>(error);
        pWriter.WriteUnicodeString(name);
        pWriter.WriteUnicodeString(message);

        return pWriter;
    }

    public static ByteWriter Accept(Buddy buddy) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.Accept);
        pWriter.Write<BuddyError>(BuddyError.ok);
        pWriter.WriteLong(buddy.Id);
        pWriter.WriteLong(buddy.BuddyInfo.CharacterId);
        pWriter.WriteLong(buddy.BuddyInfo.AccountId);
        pWriter.WriteUnicodeString(buddy.BuddyInfo.Name);

        return pWriter;
    }

    public static ByteWriter Decline(Buddy buddy) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.Decline);
        pWriter.Write<BuddyError>(BuddyError.ok);
        pWriter.WriteLong(buddy.Id);

        return pWriter;
    }

    public static ByteWriter Block(Buddy buddy, BuddyError error = BuddyError.ok) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.Block);
        pWriter.Write<BuddyError>(error);
        pWriter.WriteLong(buddy.Id);
        pWriter.WriteUnicodeString(buddy.BuddyInfo.Name);
        pWriter.WriteUnicodeString(buddy.BlockMessage);

        return pWriter;
    }

    public static ByteWriter Unblock(Buddy buddy) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.Unblock);
        pWriter.Write<BuddyError>(BuddyError.ok);
        pWriter.WriteLong(buddy.Id);

        return pWriter;
    }

    public static ByteWriter Delete(Buddy buddy) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.Delete);
        pWriter.Write<BuddyError>(BuddyError.ok);
        pWriter.WriteLong(buddy.Id);
        pWriter.WriteLong(buddy.BuddyInfo.CharacterId);
        pWriter.WriteLong(buddy.BuddyInfo.AccountId);
        pWriter.WriteUnicodeString(buddy.BuddyInfo.Name);

        return pWriter;
    }

    public static ByteWriter UpdateInfo(Buddy buddy) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.UpdateInfo);
        pWriter.Write<BuddyError>(BuddyError.ok);
        pWriter.WriteLong(buddy.Id);
        pWriter.WriteClass<Buddy>(buddy);

        return pWriter;
    }

    public static ByteWriter Append(Buddy buddy) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.Append);
        pWriter.Write<BuddyError>(BuddyError.ok);
        pWriter.WriteLong(buddy.Id);
        pWriter.WriteClass<Buddy>(buddy);

        return pWriter;
    }

    public static ByteWriter UpdateBlock(Buddy buddy, BuddyError error = BuddyError.ok) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.UpdateBlock);
        pWriter.Write<BuddyError>(error);
        pWriter.WriteLong(buddy.Id);
        pWriter.WriteUnicodeString(buddy.BuddyInfo.Name);
        pWriter.WriteUnicodeString(buddy.BlockMessage);

        return pWriter;
    }

    public static ByteWriter NotifyAccept(Buddy buddy) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.NotifyAccept);
        pWriter.WriteLong(buddy.Id);

        return pWriter;
    }

    public static ByteWriter NotifyBlock(BuddyError error = BuddyError.ok) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.NotifyBlock);
        pWriter.Write<BuddyError>(error);

        return pWriter;
    }

    public static ByteWriter NotifyDelete(Buddy buddy, string action) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.NotifyDelete);
        pWriter.WriteInt();
        pWriter.WriteUnicodeString(buddy.BuddyInfo.Name);
        pWriter.WriteUnicodeString(action);
        pWriter.WriteLong(buddy.Id);

        return pWriter;
    }

    public static ByteWriter NotifyOnline(Buddy buddy) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.NotifyOnline);
        pWriter.WriteBool(buddy.BuddyInfo.Online);
        pWriter.WriteLong(buddy.Id);
        pWriter.WriteUnicodeString(buddy.BuddyInfo.Name);

        return pWriter;
    }

    public static ByteWriter StartList() {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.StartList);

        return pWriter;
    }

    public static ByteWriter Cancel(Buddy buddy) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.Cancel);
        pWriter.Write<BuddyError>(BuddyError.ok);
        pWriter.WriteLong(buddy.Id);

        return pWriter;
    }

    // s_ban_check_err_any: Contains a forbidden word.
    public static ByteWriter Forbidden() {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.Forbidden);

        return pWriter;
    }

    public static ByteWriter EndList(int count) {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.EndList);
        pWriter.WriteInt(count);

        return pWriter;
    }

    public static ByteWriter Unknown() {
        var pWriter = Packet.Of(SendOp.BUDDY);
        pWriter.Write<Command>(Command.Unknown);

        return pWriter;
    }
}
