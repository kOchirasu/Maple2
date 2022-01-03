using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Login.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Login.PacketHandlers;

public class RequestTimeSyncHandler : RequestTimeSyncHandler<LoginSession> {
    public RequestTimeSyncHandler(ILogger<RequestTimeSyncHandler> logger) : base(logger) { }
}
