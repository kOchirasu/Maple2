using System.Text;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Core.PacketHandlers;

// Note: socket_exception debug offset includes +6 bytes from encrypted header
public abstract class LogSendHandler<T> : IPacketHandler<T> where T : Session {
    public ushort OpCode => RecvOp.LOG_SEND;

    private readonly ILogger logger;

    private enum Type : byte {
        Log = 0,
        Metric = 1,
    }

    protected LogSendHandler(ILogger logger) {
        this.logger = logger;
    }

    public virtual void Handle(T session, IByteReader packet) {
        byte unknown = packet.ReadByte();
        if (unknown != 0) {
            return;
        }

        var type = packet.Read<Type>();
        switch (type) {
            case Type.Log:
                var builder = new StringBuilder();
                string message = packet.ReadUnicodeString();
                builder.Append(message);
                if (message.Contains("exception")) {
                    // Read remaining string
                    string debug = packet.ReadUnicodeString();
                    logger.LogError("[{Message}] {Debug}", message, debug);

                    session.OnError?.Invoke(session, debug);
                    return;
                } else {
                    message = packet.ReadUnicodeString();
                    builder.Append(message);
                }

                logger.LogInformation("Client Log: {Builder}", builder);
                break;
            case Type.Metric:
                byte count = packet.ReadByte();
                for (byte i = 0; i < count; i++) {
                    packet.ReadString(); // StatisticType
                    packet.Read<float>(); // Average
                    packet.Read<float>(); // Standard Deviation
                    packet.Read<int>(); // Data Points
                    packet.Read<float>(); // Min
                    packet.Read<float>(); // Max
                }
                break;
        }
    }

    public abstract override string ToString();
}