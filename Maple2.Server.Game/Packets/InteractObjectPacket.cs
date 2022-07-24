using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class InteractObjectPacket {
    private enum Command : byte {
        Update = 4,
        Interact = 5,
        SetState = 6,
        Unknown7 = 7,
        Load = 8,
        Add = 9,
        Remove = 10,
        Result = 13,
        Unknown14 = 14,
        Hold = 15,
    }

    public static ByteWriter Update(IInteractObject interact) {
        var pWriter = Packet.Of(SendOp.InteractObject);
        pWriter.Write<Command>(Command.Update);
        pWriter.WriteString(interact.EntityId);
        pWriter.Write<InteractState>(interact.State);
        pWriter.Write<InteractType>(interact.Type);

        return pWriter;
    }

    public static ByteWriter Interact(IInteractObject interact, short gatherResult = 0, int decreaseAmount = 0) {
        var pWriter = Packet.Of(SendOp.InteractObject);
        pWriter.Write<Command>(Command.Interact);
        pWriter.WriteString(interact.EntityId);
        pWriter.Write<InteractType>(interact.Type);

        if (interact.Type == InteractType.Gathering) {
            pWriter.WriteShort(gatherResult);
            pWriter.WriteInt(decreaseAmount);
        }

        return pWriter;
    }

    public static ByteWriter SetState(IInteractObject interact) {
        var pWriter = Packet.Of(SendOp.InteractObject);
        pWriter.Write<Command>(Command.SetState);
        pWriter.WriteInt(interact.Id);
        pWriter.Write<InteractState>(interact.State);

        return pWriter;
    }

    public static ByteWriter Unknown7(byte value) {
        var pWriter = Packet.Of(SendOp.InteractObject);
        pWriter.Write<Command>(Command.Unknown7);
        pWriter.WriteByte(value);

        return pWriter;
    }

    public static ByteWriter Load(ICollection<IInteractObject> interacts) {
        var pWriter = Packet.Of(SendOp.InteractObject);
        pWriter.Write<Command>(Command.Load);
        pWriter.WriteInt(interacts.Count);
        foreach (IInteractObject interact in interacts) {
            pWriter.WriteString(interact.EntityId);
            pWriter.Write<InteractState>(interact.State);
            pWriter.Write<InteractType>(interact.Type);

            if (interact is InteractGatheringObject gathering) {
                pWriter.WriteInt(gathering.Count);   // RemainGatherCount
            }
        }

        return pWriter;
    }

    public static ByteWriter Add(IInteractObject interact) {
        var pWriter = Packet.Of(SendOp.InteractObject);
        pWriter.Write<Command>(Command.Add);
        pWriter.WriteClass<IInteractObject>(interact);

        return pWriter;
    }

    public static ByteWriter Remove(string entityId, string effect = "") {
        var pWriter = Packet.Of(SendOp.InteractObject);
        pWriter.Write<Command>(Command.Remove);
        pWriter.WriteString(entityId);
        pWriter.WriteUnicodeString(effect);

        return pWriter;
    }

    public static ByteWriter Result(InteractResult result, IInteractObject interact) {
        var pWriter = Packet.Of(SendOp.InteractObject);
        pWriter.Write<Command>(Command.Result);
        pWriter.Write<InteractResult>(result);
        pWriter.WriteString(interact.EntityId);
        pWriter.Write<InteractType>(interact.Type);

        return pWriter;
    }

    public static ByteWriter Unknown14() {
        var pWriter = Packet.Of(SendOp.InteractObject);
        pWriter.Write<Command>(Command.Unknown14);
        pWriter.WriteShort(1);
        pWriter.WriteInt(); // MapId
        pWriter.WriteInt(); // StringTable
        pWriter.WriteUnicodeString(); // StringCode

        return pWriter;
    }

    public static ByteWriter Hold(int objectId, int itemId) {
        var pWriter = Packet.Of(SendOp.InteractObject);
        pWriter.Write<Command>(Command.Hold);
        pWriter.WriteInt(objectId);
        pWriter.WriteInt(itemId);

        return pWriter;
    }
}
