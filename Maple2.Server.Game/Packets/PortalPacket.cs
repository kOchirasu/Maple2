using System.Numerics;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;

namespace Maple2.Server.Game.Packets;

public static class PortalPacket {
    private enum Command : byte {
        Add = 0,
        Remove = 1,
        Trigger = 2,
        Move = 3,
    }

    public static ByteWriter Add(FieldEntity<Portal> fieldPortal) {
        Portal portal = fieldPortal;

        var pWriter = Packet.Of(SendOp.FIELD_PORTAL);
        pWriter.Write<Command>(Command.Add);
        pWriter.WriteInt(portal.Id);
        pWriter.WriteBool(portal.Visible);
        pWriter.WriteBool(portal.Enable);
        pWriter.Write<Vector3>(fieldPortal.Position);
        pWriter.Write<Vector3>(fieldPortal.Rotation);
        pWriter.Write<Vector3>(portal.Dimension);
        pWriter.WriteUnicodeString(); // Model: Eff_Com_Portal_E
        pWriter.WriteInt(portal.TargetMapId);
        pWriter.WriteInt(fieldPortal.ObjectId);
        pWriter.WriteInt();
        pWriter.WriteBool(portal.MinimapVisible);
        pWriter.WriteLong();
        pWriter.WriteByte(portal.Type);
        pWriter.WriteInt(); // StartTick
        pWriter.WriteShort();
        pWriter.WriteInt(); // EndTick
        pWriter.WriteBool(false); // locked?
        pWriter.WriteUnicodeString(); // Owner name
        pWriter.WriteUnicodeString();
        pWriter.WriteUnicodeString();

        return pWriter;
    }

    public static ByteWriter Remove(int objectId) {
        var pWriter = Packet.Of(SendOp.FIELD_PORTAL);
        pWriter.Write<Command>(Command.Remove);
        pWriter.WriteInt(objectId);

        return pWriter;
    }

    public static ByteWriter Trigger(bool visible, bool enabled, bool minimapVisible, short unknown) {
        var pWriter = Packet.Of(SendOp.FIELD_PORTAL);
        pWriter.Write<Command>(Command.Trigger);
        pWriter.WriteBool(visible);
        pWriter.WriteBool(enabled);
        pWriter.WriteBool(minimapVisible);
        pWriter.WriteShort(unknown);

        return pWriter;
    }

    public static ByteWriter Move(FieldEntity<Portal> fieldPortal) {
        var pWriter = Packet.Of(SendOp.FIELD_PORTAL);
        pWriter.Write<Command>(Command.Move);
        pWriter.Write<Vector3>(fieldPortal.Position);
        pWriter.Write<Vector3>(fieldPortal.Rotation);

        return pWriter;
    }

    public static ByteWriter MoveByPortal(IActor actor, Portal portal) {
        var pWriter = Packet.Of(SendOp.USER_MOVE_BY_PORTAL);
        pWriter.WriteInt(actor.ObjectId);
        pWriter.Write<Vector3>(portal.Position + new Vector3(0, 0, 25)); // Always seems to be offset by 25
        pWriter.Write<Vector3>(portal.Rotation);
        pWriter.WriteBool(true); // isPortal?

        return pWriter;
    }
}
