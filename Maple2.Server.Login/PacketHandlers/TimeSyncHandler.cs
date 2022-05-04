using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Login.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Login.PacketHandlers;

public class TimeSyncHandler : TimeSyncHandler<LoginSession> {
    public TimeSyncHandler(ILogger<TimeSyncHandler> logger) : base(logger) { }
}
