using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Model.State;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class NpcControlPacket {
    public const short ANI_JUMP_A = -2;
    public const short ANI_JUMP_B = -3;

    public static ByteWriter Control(params FieldNpc[] npcs) {
        var pWriter = Packet.Of(SendOp.NpcControl);
        pWriter.WriteShort((short) npcs.Length);

        foreach (FieldNpc npc in npcs) {
            using var buffer = new PoolByteWriter();
            buffer.NpcBuffer(npc);
            pWriter.WriteShort((short) buffer.Length);
            pWriter.WriteBytes(buffer.ToArray());
        }

        return pWriter;
    }

    private static void NpcBuffer(this PoolByteWriter buffer, FieldNpc npc, float sequenceSpeed = 1f) {
        buffer.WriteInt(npc.ObjectId);
        // Flags bit-1 (AdditionalEffectRelated), bit-2 (UIHpBarRelated)
        buffer.WriteByte(2);

        buffer.Write<Vector3S>(npc.Position);
        buffer.WriteShort((short) (npc.Rotation.Z * 10));
        buffer.Write<Vector3S>(npc.Velocity.Rotate(npc.Rotation));
        buffer.WriteShort((short) (sequenceSpeed * 100));

        if (npc.Value.IsBoss) {
            buffer.WriteInt(npc.TargetId); // ObjectId of Player being targeted?
        }

        buffer.Write<ActorState>(npc.State.State);
        buffer.WriteShort(npc.SequenceId);
        buffer.WriteShort(npc.SequenceCounter);

        // Animation (-2 = Jump_A, -3 = Jump_B)
        if (npc.SequenceId is ANI_JUMP_A or ANI_JUMP_B && npc.State is StateJumpNpc jump) {
            buffer.WriteClass<StateJumpNpc>(jump);
        }

        switch (npc.State) {
            case StateHitNpc hit:
                buffer.WriteClass<StateHitNpc>(hit);
                break;
            case StateSpawn spawn:
                buffer.WriteClass<StateSpawn>(spawn);
                break;
        }

        // Set -1 to continue previous animation
        npc.SequenceId = -1;
    }
}
