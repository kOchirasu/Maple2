using System.ComponentModel;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class GuildInvite : IByteSerializable, IByteDeserializable {
    public enum Response : byte {
        Accept = 0,
        [Description("{0} has rejected the guild invitation.")]
        RejectInvite = 1,
        [Description("{0} cannot receive the guild invitation at the moment.")]
        RejectLogout = 2,
        [Description("{0} cannot receive the guild invitation at the moment.")]
        RejectTimeout = 3,
        [Description("Enter at least 2 letters.")]
        InvalidName = byte.MaxValue,
    }

    public long GuildId { get; set; }
    public string GuildName { get; set; } = string.Empty;
    public string Unknown { get; set; } = string.Empty;
    public string LeaderName { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;

    public void WriteTo(IByteWriter writer) {
        writer.WriteLong(GuildId);
        writer.WriteUnicodeString(GuildName);
        writer.WriteUnicodeString(Unknown);
        writer.WriteUnicodeString(LeaderName);
        writer.WriteUnicodeString(PlayerName);
    }

    public void ReadFrom(IByteReader reader) {
        GuildId = reader.ReadLong();
        GuildName = reader.ReadUnicodeString();
        Unknown = reader.ReadUnicodeString();
        LeaderName = reader.ReadUnicodeString();
        PlayerName = reader.ReadUnicodeString();
    }
}
