using System;
using System.Numerics;
using System.Threading;
using Maple2.Model.Common;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game;

public class PartyMember : IByteSerializable, IDisposable {
    public long PartyId { get; init; }
    public required PlayerInfo Info;
    public long JoinTime;
    public long LoginTime;
    public long AccountId => Info.AccountId;
    public long CharacterId => Info.CharacterId;
    public string Name => Info.Name;
    public byte ReadyState = 0;

    public CancellationTokenSource? TokenSource;

    public void WriteTo(IByteWriter writer) {
        writer.WriteLong(Info.AccountId);
        writer.WriteLong(Info.CharacterId);
        writer.WriteUnicodeString(Info.Name);
        writer.Write(Info.Gender);
        writer.WriteByte(1);
        writer.WriteLong();
        writer.WriteInt();
        writer.WriteInt(Info.MapId);
        writer.WriteInt(Info.MapId);
        writer.WriteInt(Info.PlotMapId);
        writer.WriteShort(Info.Level);
        writer.WriteShort(Info.Channel);
        writer.WriteInt((int) Info.Job.Code());
        writer.Write(Info.Job);
        writer.WriteInt((int) Info.CurrentHp);
        writer.WriteInt((int) Info.TotalHp);
        writer.WriteShort();
        writer.WriteLong();
        writer.WriteLong();
        writer.WriteLong();
        writer.WriteInt();
        writer.Write(new Vector3());
        writer.WriteInt(Info.GearScore);
        writer.Write(new SkinColor());
        writer.WriteLong();
        writer.Write(new AchievementInfo());
        writer.WriteLong();
        writer.WriteUnicodeString();
        writer.WriteUnicodeString(Info.Motto);
        writer.WriteUnicodeString(Info.Picture);
        writer.WriteByte();
        writer.WriteByte();
        writer.WriteClass(new Mastery());
        writer.WriteUnicodeString();
        writer.WriteLong();
        writer.WriteLong();
        writer.WriteLong();
        writer.WriteInt();
        writer.WriteByte();
        writer.WriteBool(false);
        writer.WriteLong();
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        writer.WriteInt();
        writer.WriteLong();
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteShort();
        writer.WriteLong();
    }

    public void WriteDungeonEligibility(IByteWriter writer) {
        var Count = 0;
        writer.WriteInt(Count);
        for (var i = 0; i < Count; i++) {
            writer.WriteInt();
            writer.WriteByte();
        }
    }

    public void Dispose() {
        TokenSource?.Cancel();
        TokenSource?.Dispose();
        TokenSource = null;
    }
}
