using Maple2.Server.Core.Network;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Login.Session;

public class LoginSession : Core.Network.Session {
    protected override PatchType Type => PatchType.Delete;

    public long AccountId = 0;

    public LoginSession(ILogger<LoginSession> logger) : base(logger) { }
}