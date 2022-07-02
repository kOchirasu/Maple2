using System;
using System.Numerics;
using Maple2.Model.Common;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class VibratePacket {
    private enum Command : byte {
        Attack = 1,
        Invoke = 2,
    }

    public static ByteWriter Attack(string entityId, SkillAttack attack, Vector3 position) {
        var pWriter = Packet.Of(SendOp.Vibrate);
        pWriter.Write<Command>(Command.Attack);
        pWriter.WriteString(entityId);
        pWriter.WriteLong(attack.Id);
        pWriter.WriteInt(attack.SkillId);
        pWriter.WriteShort(attack.SkillLevel);
        pWriter.WriteByte(attack.MotionPoint);
        pWriter.WriteByte(attack.AttackPoint);
        pWriter.Write<Vector3S>(position);
        pWriter.WriteInt(Environment.TickCount);
        pWriter.WriteString();
        pWriter.WriteByte();

        return pWriter;
    }

    public static ByteWriter Invoke(string entityId) {
        var pWriter = Packet.Of(SendOp.Vibrate);
        pWriter.Write<Command>(Command.Invoke);
        pWriter.WriteString(entityId);
        pWriter.WriteString();
        pWriter.WriteByte();

        return pWriter;
    }
}
