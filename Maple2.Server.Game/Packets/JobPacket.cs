using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class JobPacket {
    private enum Command : byte {
        Notify = 0,
        Basic = 1,
        Awakening = 2,
        Error = 3,
        Unknown = 7,
        Load = 8,
        Update = 9,
        Reset = 10,
        AutoDistribute = 11,
    }

    // Don't know what this actually is used for, but it should create UI component.
    public static ByteWriter Notify(IActor<Player> player, JobInfo jobInfo) {
        var pWriter = Packet.Of(SendOp.JOB);
        pWriter.Write(player.ObjectId);
        pWriter.Write<Command>(Command.Notify);
        pWriter.WriteClass<JobInfo>(jobInfo);

        return pWriter;
    }

    public static ByteWriter Basic(IActor<Player> player, JobInfo jobInfo) {
        var pWriter = Packet.Of(SendOp.JOB);
        pWriter.Write(player.ObjectId);
        pWriter.Write<Command>(Command.Basic);
        pWriter.WriteClass<JobInfo>(jobInfo);

        return pWriter;
    }

    public static ByteWriter Awakening(IActor<Player> player, JobInfo jobInfo) {
        var pWriter = Packet.Of(SendOp.JOB);
        pWriter.Write(player.ObjectId);
        pWriter.Write<Command>(Command.Awakening);
        pWriter.WriteClass<JobInfo>(jobInfo);

        return pWriter;
    }

    public static ByteWriter Error(IActor<Player> player, JobError error) {
        var pWriter = Packet.Of(SendOp.JOB);
        pWriter.Write(player.ObjectId);
        pWriter.Write<Command>(Command.Error);
        pWriter.Write<JobError>(error);

        return pWriter;
    }

    // This might be related to Recv Command 7.
    public static ByteWriter Unknown(IActor<Player> player, JobInfo jobInfo) {
        var pWriter = Packet.Of(SendOp.JOB);
        pWriter.Write(player.ObjectId);
        pWriter.Write<Command>(Command.Unknown);
        pWriter.WriteClass<JobInfo>(jobInfo);

        return pWriter;
    }

    public static ByteWriter Load(IActor<Player> player, JobInfo jobInfo) {
        var pWriter = Packet.Of(SendOp.JOB);
        pWriter.Write(player.ObjectId);
        pWriter.Write<Command>(Command.Load);
        pWriter.WriteClass<JobInfo>(jobInfo);

        return pWriter;
    }

    public static ByteWriter Update(IActor<Player> player, JobInfo jobInfo) {
        var pWriter = Packet.Of(SendOp.JOB);
        pWriter.Write(player.ObjectId);
        pWriter.Write<Command>(Command.Update);
        pWriter.WriteClass<JobInfo>(jobInfo);

        return pWriter;
    }

    public static ByteWriter Reset(IActor<Player> player, JobInfo jobInfo) {
        var pWriter = Packet.Of(SendOp.JOB);
        pWriter.Write(player.ObjectId);
        pWriter.Write<Command>(Command.Reset);
        pWriter.WriteClass<JobInfo>(jobInfo);

        return pWriter;
    }

    public static ByteWriter AutoDistribute(IActor<Player> player, JobInfo jobInfo) {
        var pWriter = Packet.Of(SendOp.JOB);
        pWriter.Write(player.ObjectId);
        pWriter.Write<Command>(Command.AutoDistribute);
        pWriter.WriteClass<JobInfo>(jobInfo);

        return pWriter;
    }
}
