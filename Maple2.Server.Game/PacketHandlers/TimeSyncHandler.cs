using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.PacketHandlers;

public class TimeSyncHandler : TimeSyncHandler<GameSession> {
    public TimeSyncHandler(ILogger<TimeSyncHandler> logger) : base(logger) { }
}
