using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class Buddy : IByteSerializable {
    public long Id { get; init; }
    public long OwnerId { get; init; }
    public long LastModified { get; init; }

    public string Message = string.Empty;
    public BuddyType Type;
    public readonly PlayerInfo BuddyInfo;

    public Buddy(PlayerInfo buddyInfo) {
        BuddyInfo = buddyInfo;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteLong(Id);
        writer.WriteLong(BuddyInfo.CharacterId);
        writer.WriteLong(BuddyInfo.AccountId);
        writer.WriteUnicodeString(BuddyInfo.Name);
        writer.WriteUnicodeString(Type != BuddyType.Blocked ? Message : "");
        writer.WriteShort(); // Channel?
        writer.WriteInt(BuddyInfo.MapId);
        writer.WriteInt((int) BuddyInfo.Job.Code());
        writer.Write<Job>(BuddyInfo.Job);
        writer.WriteShort(BuddyInfo.Level);
        writer.WriteBool(Type.HasFlag(BuddyType.InRequest));
        writer.WriteBool(Type.HasFlag(BuddyType.OutRequest));
        writer.WriteBool(Type.HasFlag(BuddyType.Blocked));
        writer.WriteBool(BuddyInfo.Online);
        writer.WriteBool(false);
        writer.WriteLong(LastModified);
        writer.WriteUnicodeString(BuddyInfo.Picture);
        writer.WriteUnicodeString(BuddyInfo.Motto);
        writer.WriteUnicodeString(Type == BuddyType.Blocked ? Message : "");
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteUnicodeString(BuddyInfo.Home.Name);
        writer.WriteLong();
        writer.Write<Trophy>(BuddyInfo.Trophy);
    }
}
