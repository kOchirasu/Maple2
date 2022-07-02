using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;

namespace Maple2.Server.Game.Packets;

public static class StatsPacket {
    private enum Command : byte {
        Update = 0
    }

    public static ByteWriter Init(Actor<Player> entity) {
        Stats stats = entity.Stats;

        var pWriter = Packet.Of(SendOp.Stat);
        pWriter.WriteInt(entity.ObjectId);
        pWriter.Write<Command>(Command.Update);
        pWriter.WriteByte(Stats.TOTAL);
        for (int i = 0; i < Stats.TOTAL; i++) {
            var attribute = (StatAttribute) i;
            pWriter.WriteAttribute(attribute, stats[attribute]);
        }

        return pWriter;
    }

    public static ByteWriter Update<T>(Actor<T> entity) {
        var pWriter = Packet.Of(SendOp.Stat);
        pWriter.WriteInt(entity.ObjectId);
        pWriter.Write<Command>(Command.Update);
        switch (entity.Value) {
            case Player:
                pWriter.WritePlayerStats(entity.Stats);
                break;
            default:
                pWriter.WriteNpcStats(entity.Stats);
                break;
        }

        return pWriter;
    }

    public static ByteWriter Update<T>(Actor<T> entity, params StatAttribute[] attributes) {
        var pWriter = Packet.Of(SendOp.Stat);
        pWriter.WriteInt(entity.ObjectId);
        pWriter.Write<Command>(Command.Update);
        pWriter.WriteByte((byte) attributes.Length);
        foreach (StatAttribute attribute in attributes) {
            pWriter.WriteByte((byte) attribute);
            pWriter.WriteAttribute(attribute, entity.Stats[attribute]);
        }

        return pWriter;
    }

    #region Helpers
    public static void WritePlayerStats(this IByteWriter pWriter, Stats stats) {
        pWriter.WriteByte(Stats.TOTAL);
        for (int i = 0; i < Stat.TOTAL; i++) {
            pWriter.WriteLong(stats[StatAttribute.Health][i]);
            pWriter.WriteInt((int) stats[StatAttribute.AttackSpeed][i]);
            pWriter.WriteInt((int) stats[StatAttribute.MovementSpeed][i]);
            pWriter.WriteInt((int) stats[StatAttribute.JumpHeight][i]);
            pWriter.WriteInt((int) stats[StatAttribute.MountSpeed][i]);
        }
    }

    public static void WriteNpcStats(this IByteWriter pWriter, Stats stats) {
        pWriter.WriteByte(Stats.TOTAL);
        for (int i = 0; i < Stat.TOTAL; i++) {
            pWriter.WriteLong(stats[StatAttribute.Health][i]);
            pWriter.WriteInt((int) stats[StatAttribute.AttackSpeed][i]);
        }
    }

    private static void WriteAttribute(this IByteWriter pWriter, StatAttribute attribute, Stat stat) {
        for (int i = 0; i < Stat.TOTAL; i++) {
            if (attribute == StatAttribute.Health) {
                pWriter.WriteLong(stat[i]);
            } else {
                pWriter.WriteInt((int) stat[i]);
            }
        }
    }
    #endregion
}
