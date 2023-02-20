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

    public static ByteWriter ControlPet(params FieldPet[] pets) {
        var pWriter = Packet.Of(SendOp.NpcControl);
        pWriter.WriteShort((short) pets.Length);

        foreach (FieldPet pet in pets) {
            using var buffer = new PoolByteWriter();
            buffer.PetBuffer(pet);
            pWriter.WriteShort((short) buffer.Length);
            pWriter.WriteBytes(buffer.ToArray());
        }

        return pWriter;
    }

    // TODO: Might be able to merge this with NpcBuffer
    private static void PetBuffer(this PoolByteWriter buffer, FieldPet pet, float sequenceSpeed = 1f) {
        buffer.WriteInt(pet.ObjectId);
        buffer.WriteByte(); // Flags bit-1, bit-2
        buffer.Write<Vector3S>(pet.Position);
        buffer.WriteShort((short) (pet.Rotation.Z * 10));
        buffer.Write<Vector3S>(pet.Velocity.Rotate(pet.Rotation));
        buffer.WriteShort((short) (sequenceSpeed * 100));

        if (pet.Value.IsBoss) {
            buffer.WriteInt(pet.TargetId);
        }

        buffer.Write<ActorState>(pet.State);
        buffer.WriteShort(pet.SequenceId);
        buffer.WriteShort(pet.SequenceCounter);

        // Set -1 to continue previous animation
        pet.SequenceId = -1;
    }

    private static void NpcBuffer(this PoolByteWriter buffer, FieldNpc npc, float sequenceSpeed = 1f) {
        buffer.WriteInt(npc.ObjectId);
        buffer.WriteByte(2); // Flags bit-1 (AdditionalEffectRelated), bit-2 (UIHpBarRelated)
        buffer.Write<Vector3S>(npc.Position);
        buffer.WriteShort((short) (npc.Rotation.Z * 10));
        buffer.Write<Vector3S>(npc.Velocity.Rotate(npc.Rotation));
        buffer.WriteShort((short) (sequenceSpeed * 100));

        if (npc.Value.IsBoss) {
            buffer.WriteInt(npc.TargetId); // ObjectId of Player being targeted?
        }

        buffer.Write<ActorState>(npc.State);
        buffer.WriteShort(npc.SequenceId);
        buffer.WriteShort(npc.SequenceCounter);

        // Animation (-2 = Jump_A, -3 = Jump_B)
        if (npc.SequenceId is ANI_JUMP_A or ANI_JUMP_B && npc.StateData is StateJumpNpc jump) {
            buffer.WriteClass<StateJumpNpc>(jump);
        }

        switch (npc.StateData) {
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
