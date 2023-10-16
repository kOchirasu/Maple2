using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;
using System;

namespace Maple2.Server.Game.Packets;

public static class PartyPacket {
    private enum Command : byte {
        Error = 0,
        Joined = 2,
        Leave = 3,
        Kicked = 4,
        NotifyLogin = 5,
        NotifyLogout = 6,
        Disbanded = 7,
        NotifyUpdateLeader = 8,
        Load = 9,
        Invited = 11,
        UpdateMember = 12,
        //13 - duplicate of 12
        UpdateDungeonInfo = 14,
        Unknown1 = 15,
        Tombstone = 18,
        UpdateStats = 19,
        DungeonNotice = 20,
        Unknown2 = 21,
        DungeonReset = 25,
        PartyFinder = 26,
        PartySearch = 30,
        PartySearchDungeon = 31,
        DungeonRecord = 35,
        Unknown3 = 37,
        DungeonHelperCooldown = 40,
        JoinRequest = 44,
        StartVote = 47,
        ReadyCheck = 48,
        EndReadyCheck = 49,
        SurvivalPartySearch = 54,
    }

    public static ByteWriter Error(PartyError error, string targetName = "") {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.Error);
        pWriter.Write<PartyError>(error);
        pWriter.WriteUnicodeString(targetName);

        return pWriter;
    }

    public static ByteWriter Joined(PartyMember member) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.Joined);
        pWriter.WriteClass(member);
        member.WriteDungeonEligibility(pWriter);

        return pWriter;
    }

    public static ByteWriter Leave(long targetCharacterId, bool isSelf) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.Leave);
        pWriter.WriteLong(targetCharacterId);
        pWriter.WriteBool(isSelf);

        return pWriter;
    }

    public static ByteWriter Kicked(long targetCharacterId) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.Kicked);
        pWriter.WriteLong(targetCharacterId);

        return pWriter;
    }

    public static ByteWriter NotifyLogin(PartyMember member) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.NotifyLogin);
        pWriter.WriteClass(member);

        return pWriter;
    }

    public static ByteWriter NotifyLogout(long targetCharacterId) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.NotifyLogout);
        pWriter.WriteLong(targetCharacterId);

        return pWriter;
    }

    public static ByteWriter Disband() {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.Disbanded);

        return pWriter;
    }

    public static ByteWriter NotifyUpdateLeader(long newLeaderCharacterId) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.NotifyUpdateLeader);
        pWriter.WriteLong(newLeaderCharacterId);

        return pWriter;
    }

    public static ByteWriter Load(Party party) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.Load);
        pWriter.WriteClass<Party>(party);
        pWriter.WriteMatchParty(party);

        return pWriter;
    }

    public static ByteWriter Invite(int partyId, string name) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.Invited);

        pWriter.WriteUnicodeString(name);
        pWriter.WriteInt(partyId);

        return pWriter;
    }

    public static ByteWriter Update(PartyMember member) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.UpdateMember);

        pWriter.WriteLong(member.CharacterId);
        pWriter.WriteClass<PartyMember>(member);

        return pWriter;
    }

    public static ByteWriter UpdateDungeonInfo(PartyMember member) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.UpdateDungeonInfo);

        pWriter.Write(member.CharacterId);
        member.WriteDungeonEligibility(pWriter);

        return pWriter;
    }

    public static ByteWriter Unknown1() {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.Unknown1);

        pWriter.WriteLong();
        pWriter.WriteInt();

        return pWriter;
    }

    public static ByteWriter Tombstone() {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.Tombstone);

        pWriter.WriteLong();
        pWriter.WriteBool(true); // Dark tombstone vs light tombstone

        return pWriter;
    }

    public static ByteWriter UpdateStats(PartyMember member) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.UpdateStats);

        pWriter.WriteLong(member.CharacterId);
        pWriter.WriteLong(member.AccountId);
        pWriter.WriteInt((int) member.Info.CurrentHp);
        pWriter.WriteInt((int) member.Info.TotalHp);
        pWriter.WriteShort(member.Info.Level);

        return pWriter;
    }

    public static ByteWriter DungeonNotice() {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.DungeonNotice);

        pWriter.WriteUnicodeString(); // Notices: s_party_vote_expired|s_field_enteracne_party_notify_reset_dungeon|s_party_vote_rejected_kick_user
        pWriter.WriteUnicodeString("Field_Enterance_Reset_Dungeon");
        pWriter.WriteUnicodeString();

        return pWriter;
    }

    public static ByteWriter Unknown2(string message, bool htmlEncoded = false) {
        var text = new InterfaceText(message, htmlEncoded);
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.Unknown2);
        pWriter.WriteClass<InterfaceText>(text);
        pWriter.WriteUnicodeString(); // effect?

        return pWriter;
    }

    public static ByteWriter DungeonReset() {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.DungeonReset);
        pWriter.WriteBool(false); // started dungeon
        pWriter.WriteInt(); // dungeon id

        return pWriter;
    }

    public static ByteWriter PartyFinderListing(Party party) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.PartyFinder);
        pWriter.WriteMatchParty(party);

        return pWriter;
    }

    public static ByteWriter PartySearch(byte type, bool searching) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.PartySearch);
        /**
            if type == 1:
                dungeon_message(1) # s_enum_dungeon_group_normal
            elif type == 2:
                dungeon_message(8) # s_enum_dungeon_group_worldBoss
            elif type == 3:
                dungeon_message(11) # s_enum_dungeon_group_event
            elif type == 4:
                dungeon_message(5) # s_enum_dungeon_group_lapenta
            else:
                dungeon_message(0)
        **/
        pWriter.WriteByte(type); // Type
        pWriter.WriteBool(searching); // Searching
        pWriter.WriteBool(true); // always true?
        pWriter.WriteByte();

        return pWriter;
    }

    public static ByteWriter PartySearchDungeon() {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.PartySearchDungeon);
        pWriter.WriteLong(); // s_party_match_dungeon

        return pWriter;
    }

    public static ByteWriter DungeonRecord() {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.DungeonRecord);
        pWriter.WriteLong();

        return pWriter;
    }

    public static ByteWriter Unknown3() {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.Unknown3);
        pWriter.WriteLong();
        pWriter.WriteUnicodeString();

        return pWriter;
    }

    public static ByteWriter DungeonHelperCooldown() {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.DungeonHelperCooldown);
        pWriter.WriteInt(); // s_party_find_dungeon_helper_cooldown

        return pWriter;
    }

    public static ByteWriter JoinRequest(string name) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.JoinRequest);
        pWriter.WriteUnicodeString(name);

        return pWriter;
    }

    public static ByteWriter StartVote(bool kick, int size) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.StartVote);
        pWriter.WriteBool(kick); // 0 = Ready Check, 1 = Kick
        pWriter.WriteInt(kick ? 36 : 34);
        pWriter.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        // Party Size
        pWriter.WriteInt(size);
        for (var i = 0; i < size; i++) {
            pWriter.WriteLong(); // character id
        }

        // Ready Count
        pWriter.WriteInt(size);
        for (var i = 0; i < size; i++) {
            pWriter.WriteLong(); // character id
        }

        // Not Ready Count
        pWriter.WriteInt(size);
        for (var i = 0; i < size; i++) {
            pWriter.WriteLong(); // character id
        }

        return pWriter;
    }

    public static ByteWriter ReadyCheck(long characterId, bool isReady) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.ReadyCheck);
        pWriter.WriteLong(characterId);
        pWriter.WriteBool(isReady);

        return pWriter;
    }

    public static ByteWriter EndReadyCheck() {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.EndReadyCheck);

        return pWriter;
    }

    public static ByteWriter SurvivalPartySearch(bool searching) {
        var pWriter = Packet.Of(SendOp.Party);
        pWriter.Write<Command>(Command.SurvivalPartySearch);
        pWriter.WriteBool(searching); // Searching
        pWriter.WriteBool(true); // always true

        return pWriter;
    }

    private static void WriteMatchParty(this IByteWriter writer, Party party) {
        writer.WriteBool(party.IsMatching);
        if (party.IsMatching) {
            writer.WriteLong(party.MatchPartyId);
            writer.WriteInt(party.Id);
            writer.WriteInt(); // Unknown
            writer.WriteInt(); // Unknown
            writer.WriteUnicodeString(party.MatchPartyName);
            writer.WriteBool(party.RequireApproval);
            writer.WriteInt(party.Members.Count);
            writer.WriteInt(party.Capacity);
            writer.WriteLong(party.LeaderAccountId);
            writer.WriteLong(party.LeaderCharacterId);
            writer.WriteUnicodeString(party.LeaderName);
            writer.WriteLong(party.CreationTime);
        }
    }
}
