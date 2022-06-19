using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.PacketHandlers;
using Serilog;

namespace Maple2.Server.Core.Network;

public class PacketRouter<T> where T : Session {
    private readonly ILogger logger = Log.Logger.ForContext<T>();
    private readonly ImmutableDictionary<ushort, PacketHandler<T>> handlers;

    public PacketRouter(IEnumerable<PacketHandler<T>> packetHandlers) {
        var builder = ImmutableDictionary.CreateBuilder<ushort, PacketHandler<T>>();
        foreach (PacketHandler<T> packetHandler in packetHandlers.OrderBy(handler => handler.OpCode)) {
            Register(builder, packetHandler);
        }
        handlers = builder.ToImmutable();
    }

    public void OnPacket(object? sender, IByteReader reader) {
        ushort op = reader.Read<ushort>();
        PacketHandler<T>? handler = handlers.GetValueOrDefault(op);
        if (sender is T session) {
            handler?.Handle(session, reader);
        }
    }

    private void Register(ImmutableDictionary<ushort, PacketHandler<T>>.Builder builder, PacketHandler<T> packetHandler) {
        logger.Debug("Registered [{OpCode}] {Name}", $"0x{packetHandler.OpCode:X4}", packetHandler.GetType().Name);
        builder.Add(packetHandler.OpCode, packetHandler);
    }
}
