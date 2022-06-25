using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;
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

    public static ByteWriter ControlPet(params FieldNpc[] pets) {
        var pWriter = Packet.Of(SendOp.NpcControl);
        pWriter.WriteShort((short) pets.Length);

        foreach (FieldNpc pet in pets) {
            using var buffer = new PoolByteWriter();
            buffer.PetBuffer(pet);
            pWriter.WriteShort((short) buffer.Length);
            pWriter.WriteBytes(buffer.ToArray());
        }

        return pWriter;
    }

    private static void PetBuffer(this PoolByteWriter buffer, FieldNpc pet, float sequenceSpeed = 1) {
        buffer.WriteInt(pet.ObjectId);
        buffer.WriteByte(); // Flags bit-1, bit-2
        buffer.Write<Vector3S>(pet.Position);
        buffer.WriteShort((short) (pet.Rotation.Z * 10));
        buffer.Write<Vector3S>(default); // XYZ Speed
        buffer.WriteShort((short) (sequenceSpeed * 100));

        if (pet.Value.Metadata.Basic.Friendly == 0 && pet.Value.Metadata.Basic.Class >= 3) {
            buffer.WriteInt(); // TargetId
        }

        byte flag = 1;
        buffer.WriteByte(flag);
        buffer.WriteShort(-1);
        buffer.WriteShort(1);
    }

    private static void NpcBuffer(this PoolByteWriter buffer, FieldNpc npc, float sequenceSpeed = 1) {
        buffer.WriteInt(npc.ObjectId);
        buffer.WriteByte(2); // Flags bit-1 (AdditionalEffectRelated), bit-2 (UIHpBarRelated)
        buffer.Write<Vector3S>(npc.Position);
        buffer.WriteShort((short) (npc.Rotation.Z * 10));
        buffer.Write<Vector3S>(npc.Velocity.Rotate(npc.Rotation));
        buffer.WriteShort((short) (sequenceSpeed * 100));

        if (npc.Value.Metadata.Basic.Friendly == 0 && npc.Value.Metadata.Basic.Class >= 3) {
            buffer.WriteInt(npc.TargetId); // ObjectId of Player being targeted?
        }

        buffer.Write<ActorState>(npc.State);
        buffer.WriteShort(npc.SequenceId);
        buffer.WriteShort(npc.SequenceCounter);

        // Animation (-2 = Jump_A, -3 = Jump_B)
        if (npc.SequenceId is ANI_JUMP_A or ANI_JUMP_B && npc.StateData is NpcStateJump jump) {
            buffer.WriteClass<NpcStateJump>(jump);
        }

        switch (npc.StateData) {
            case NpcStateHit hit:
                buffer.WriteClass<NpcStateHit>(hit);
                break;
            case NpcStateSpawn spawn:
                buffer.WriteClass<NpcStateSpawn>(spawn);
                break;
        }

        // Set -1 to continue previous animation
        npc.SequenceId = -1;
    }
}
