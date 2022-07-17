using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Server.Game.Model.Skill;

public class TargetRecord : IByteSerializable {
    public int AttackCounter;
    public int CasterId;
    public int TargetId;
    public byte Index;
    public byte Unknown;

    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(AttackCounter);
        writer.WriteInt(CasterId);
        writer.WriteInt(TargetId);
        writer.WriteByte(Index);
        writer.WriteByte(Unknown);
    }
}
