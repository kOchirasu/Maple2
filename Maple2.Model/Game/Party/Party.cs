using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game;

public class Party : IByteSerializable {
    private int capacity = Constant.PartyMaxCapacity;
    public int Capacity { get => capacity; set { capacity = Math.Clamp(value, Constant.PartyMinCapacity, Constant.PartyMaxCapacity); } }

    public required int Id { get; init; }
    public required long LeaderAccountId;
    public required long LeaderCharacterId;
    public required string LeaderName;
    public long CreationTime;
    public int DungeonId = 0;
    public string MatchPartyName = "";
    public int MatchPartyId = 0;
    public bool IsMatching = false;
    public bool RequireApproval = false;
    public readonly ConcurrentDictionary<long, PartyMember> Members;

    [SetsRequiredMembers]
    public Party(int id, long leaderAccountId, long leaderCharacterId, string leaderName) {
        Id = id;
        LeaderAccountId = leaderAccountId;
        LeaderCharacterId = leaderCharacterId;
        LeaderName = leaderName;
        CreationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Members = new ConcurrentDictionary<long, PartyMember>();
    }

    [SetsRequiredMembers]
    public Party(int id, PartyMember leader) : this(id, leader.AccountId, leader.CharacterId, leader.Name) { }

    public void WriteTo(IByteWriter writer) {
        writer.WriteBool(true);
        writer.WriteInt(Id);
        writer.WriteLong(LeaderCharacterId);

        byte memberCount = (byte) Members.Count;
        writer.WriteByte(memberCount);
        foreach (PartyMember member in Members.Values) {
            writer.WriteBool(!member.Info.Online);
            writer.WriteClass(member);
            member.WriteDungeonEligibility(writer);
        }

        writer.WriteBool(false);
        writer.WriteInt(DungeonId);
        writer.WriteBool(false);
        writer.WriteByte();
        WriteMatchParty(writer);
    }

    public void WriteMatchParty(IByteWriter writer) {
        writer.WriteBool(IsMatching);
        if (!IsMatching) {
            return;
        }

        writer.WriteLong(MatchPartyId);
        writer.WriteInt(Id);
        writer.WriteInt(); // Unknown
        writer.WriteInt(); // Unknown
        writer.WriteUnicodeString(MatchPartyName);
        writer.WriteBool(RequireApproval);
        writer.WriteInt(Members.Count);
        writer.WriteInt(Capacity);
        writer.WriteLong(LeaderAccountId);
        writer.WriteLong(LeaderCharacterId);
        writer.WriteUnicodeString(LeaderName);
        writer.WriteLong(CreationTime);
    }
}
