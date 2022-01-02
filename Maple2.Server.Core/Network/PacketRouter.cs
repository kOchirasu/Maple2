using System.Collections.Generic;
using System.Collections.Immutable;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.PacketHandlers;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Core.Network;

public class PacketRouter<T> where T : Session {
    private readonly ImmutableDictionary<ushort, IPacketHandler<T>> handlers;
    private readonly ILogger logger;

    public PacketRouter(IEnumerable<IPacketHandler<T>> packetHandlers, ILogger<PacketRouter<T>> logger) {
        this.logger = logger;

        var builder = ImmutableDictionary.CreateBuilder<ushort, IPacketHandler<T>>();
        foreach (IPacketHandler<T> packetHandler in packetHandlers) {
            Register(builder, packetHandler);
        }
        this.handlers = builder.ToImmutable();
    }

    public void OnPacket(object? sender, IByteReader reader) {
        ushort op = reader.Read<ushort>();
        IPacketHandler<T>? handler = handlers.GetValueOrDefault(op);
        if (sender is T session) {
            handler?.Handle(session, reader);
        }
    }

    private void Register(ImmutableDictionary<ushort, IPacketHandler<T>>.Builder builder, IPacketHandler<T> packetHandler) {
        logger.LogDebug("Registered {Handler}", packetHandler);
        builder.Add(packetHandler.OpCode, packetHandler);
    }
}